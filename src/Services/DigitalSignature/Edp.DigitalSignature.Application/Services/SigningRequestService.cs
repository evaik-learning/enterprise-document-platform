namespace Edp.DigitalSignature.Application.Services;

using System.Security.Cryptography;
using System.Diagnostics;
using System.Text.Json;
using Edp.DigitalSignature.Application.Contracts;
using Edp.DigitalSignature.Application.Interfaces;
using Edp.DigitalSignature.Domain.Entities;
using Edp.DigitalSignature.Domain.Enums;
using Edp.DigitalSignature.Domain.Exceptions;
using Edp.Shared.Security.CurrentUser;
using Edp.DigitalSignature.Application.Telemetry;

public sealed class SigningRequestService : ISigningRequestService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentOrganization _currentOrganization;
    private readonly ISigningAuditService _auditService;
    private readonly ISignatureProviderResolver _providerResolver;
    private readonly IDocumentContentStore _documentContentStore;

    public SigningRequestService(
        IUnitOfWork unitOfWork,
        ICurrentOrganization currentOrganization,
        ISigningAuditService auditService,
        ISignatureProviderResolver providerResolver,
        IDocumentContentStore documentContentStore)
    {
        _unitOfWork = unitOfWork;
        _currentOrganization = currentOrganization;
        _auditService = auditService;
        _providerResolver = providerResolver;
        _documentContentStore = documentContentStore;
    }

    public async Task<SigningRequestDto> CreateSigningRequestAsync(CreateSigningRequestCommand request, CancellationToken cancellationToken = default)
    {
        using var activity = SigningTelemetry.ActivitySource.StartActivity("signing.create");
        var organizationId = _currentOrganization.OrganizationId
            ?? throw new InvalidOperationException("An organization context is required.");

        if (request.Signers.Count == 0)
        {
            throw new ArgumentException("At least one signer is required.", nameof(request));
        }

        var signingRequest = SigningRequest.Create(
            organizationId,
            request.WorkflowInstanceId,
            request.DocumentId,
            request.DocumentVersionId,
            request.SigningMode,
            request.Title,
            request.Message,
            request.ExpiresAt,
            request.DocumentHash,
            request.Provider,
            request.CorrelationId == Guid.Empty ? Guid.NewGuid() : request.CorrelationId);

        var signers = request.Signers.Select(s => Signer.Create(
            signingRequest.SigningRequestId,
            s.UserId,
            s.Email,
            s.DisplayName,
            s.Role,
            s.SigningOrder,
            s.IsRequired)).ToList();

        var signerIds = signers.Select(s => s.SignerId).ToHashSet();
        var fields = request.SignatureFields.Select(field =>
        {
            if (!signerIds.Contains(field.SignerId))
            {
                throw new ArgumentException($"Signature field references unknown signer '{field.SignerId}'.", nameof(request));
            }

            return SignatureField.Create(
                signingRequest.SigningRequestId,
                field.SignerId,
                field.DocumentPage,
                field.FieldType,
                field.X,
                field.Y,
                field.Width,
                field.Height,
                field.Required,
                field.Label);
        }).ToList();

        _unitOfWork.SigningRequests.Add(signingRequest);
        _unitOfWork.Signers.AddRange(signers);
        _unitOfWork.SignatureFields.AddRange(fields);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        SigningTelemetry.RequestsCreated.Add(1);

        return MapSummary(signingRequest, signers);
    }

    public async Task ActivateSigningRequestAsync(Guid signingRequestId, CancellationToken cancellationToken = default)
    {
        using var activity = SigningTelemetry.ActivitySource.StartActivity("signing.activate");
        var request = await GetRequestAsync(signingRequestId, cancellationToken);
        if (string.IsNullOrWhiteSpace(request.ProviderRequestId))
        {
            await CreateProviderEnvelopeAsync(request, cancellationToken);
        }

        request.Activate();
        foreach (var signer in request.Signers)
        {
            signer.MarkAsInvited();
            _unitOfWork.Signers.Update(signer);
        }

        _unitOfWork.SigningRequests.Update(request);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task CreateProviderEnvelopeAsync(SigningRequest request, CancellationToken cancellationToken)
    {
        var startedAt = Stopwatch.GetTimestamp();
        var originalDocument = await _documentContentStore.DownloadOriginalAsync(
            request.OrganizationId,
            request.DocumentId,
            request.DocumentVersionId,
            cancellationToken)
            ?? throw new InvalidOperationException("The original document content could not be found.");

        await using var document = originalDocument;
        using var buffer = new MemoryStream();
        await document.CopyToAsync(buffer, cancellationToken);
        var documentBytes = buffer.ToArray();
        var actualHash = Convert.ToHexString(SHA256.HashData(documentBytes));
        if (!string.Equals(actualHash, request.DocumentHash, StringComparison.OrdinalIgnoreCase))
        {
            throw new DocumentVersionMismatchException(request.DocumentVersionId, request.DocumentHash, actualHash);
        }

        var signers = request.Signers.ToList();
        var fields = await _unitOfWork.SignatureFields.GetBySigningRequestIdAsync(request.SigningRequestId, cancellationToken);
        var provider = _providerResolver.Resolve(request.Provider);
        var envelopeRequest = new SignatureEnvelopeRequest
        {
            IdempotencyKey = request.SigningRequestId.ToString("N"),
            Title = request.Title,
            Message = request.Message,
            DocumentBytes = documentBytes,
            Signers = signers.Select(s => new SignerEnvelopeInfo
            {
                SignerId = s.SignerId.ToString(),
                Email = s.Email,
                DisplayName = s.DisplayName,
                SigningOrder = s.SigningOrder
            }).ToList(),
            Fields = fields.Select(field => new SignatureFieldInfo
            {
                SignerId = field.SignerId.ToString(),
                PageNumber = field.DocumentPage,
                X = field.X,
                Y = field.Y,
                Width = field.Width,
                Height = field.Height,
                FieldType = field.FieldType.ToString(),
                Required = field.Required
            }).ToList()
        };

        var result = await provider.CreateEnvelopeAsync(envelopeRequest, cancellationToken);
        SigningTelemetry.ProviderLatency.Record(Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds, new KeyValuePair<string, object?>("operation", "create-envelope"));
        if (string.IsNullOrWhiteSpace(result.ProviderRequestId))
        {
            throw new InvalidOperationException("The signature provider did not return a request ID.");
        }

        var transaction = SigningProviderTransaction.Create(
            request.SigningRequestId,
            request.Provider,
            result.ProviderRequestId,
            ProviderRequestType.CreateEnvelope,
            JsonSerializer.Serialize(new
            {
                envelopeRequest.Title,
                envelopeRequest.IdempotencyKey,
                SignerCount = envelopeRequest.Signers.Count,
                FieldCount = envelopeRequest.Fields.Count,
                DocumentHash = request.DocumentHash
            }));
        transaction.RecordSuccess(JsonSerializer.Serialize(result));
        _unitOfWork.ProviderTransactions.Add(transaction);
        request.SetProviderRequestId(result.ProviderRequestId);
    }

    public async Task<SigningRequestDetailDto> GetSigningRequestAsync(Guid signingRequestId, CancellationToken cancellationToken = default)
    {
        var request = await GetRequestAsync(signingRequestId, cancellationToken, detailed: true);
        var fields = await _unitOfWork.SignatureFields.GetBySigningRequestIdAsync(signingRequestId, cancellationToken);
        var auditTrail = await _auditService.GetAuditTrailAsync(signingRequestId, cancellationToken);
        return MapDetail(request, fields, auditTrail);
    }

    public async Task<PagedResult<SigningRequestDto>> ListSigningRequestsAsync(ListSigningRequestsFilter filter, CancellationToken cancellationToken = default)
    {
        filter.Page = Math.Max(1, filter.Page);
        filter.PageSize = Math.Clamp(filter.PageSize, 1, 100);
        var requests = await _unitOfWork.SigningRequests.ListAsync(filter, cancellationToken);
        var totalCount = await _unitOfWork.SigningRequests.CountAsync(filter, cancellationToken);
        return new PagedResult<SigningRequestDto>
        {
            Items = requests.Select(request => MapSummary(request, request.Signers)).ToList(),
            Page = filter.Page,
            PageSize = filter.PageSize,
            TotalCount = totalCount
        };
    }

    public async Task SignAsync(Guid signingRequestId, Guid signerId, SignRequest request, CancellationToken cancellationToken = default)
    {
        using var activity = SigningTelemetry.ActivitySource.StartActivity("signing.sign");
        var signingRequest = await GetRequestAsync(signingRequestId, cancellationToken, detailed: true);
        var signer = signingRequest.Signers.FirstOrDefault(s => s.SignerId == signerId)
            ?? throw new SignerNotFoundException(signerId);

        if (!signingRequest.CanSign())
        {
            throw new InvalidSigningStateException(signingRequest.Status.ToString(), nameof(SignAsync));
        }

        if (signer.Status == SignerStatus.Signed)
        {
            throw new DuplicateSignatureException(signingRequestId, signerId);
        }

        if (signingRequest.SigningMode == SigningMode.Sequential)
        {
            var nextSigner = signingRequest.Signers
                .Where(s => s.Status != SignerStatus.Signed)
                .OrderBy(s => s.SigningOrder)
                .FirstOrDefault();
            if (nextSigner?.SignerId != signerId)
            {
                throw new InvalidSigningOrderException("The signer is not next in the sequential signing order.");
            }
        }

        if (signingRequest.Status == SigningRequestStatus.Pending)
        {
            signingRequest.StartSigning();
        }

        await SignWithProviderAsync(signingRequest, signer, request, cancellationToken);
        signer.MarkAsSigned();
        _unitOfWork.Signers.Update(signer);
        await _auditService.RecordActionAsync(signingRequestId, signerId, nameof(SignatureActionType.Signed), request.IpAddress, request.UserAgent, cancellationToken);

        var requiredSigners = signingRequest.Signers.Where(s => s.IsRequired).ToList();
        if (requiredSigners.All(s => s.Status == SignerStatus.Signed))
        {
            signingRequest.CompleteSigning();
            await PersistCompletedDocumentAsync(signingRequest, cancellationToken);
            SigningTelemetry.RequestsCompleted.Add(1);
        }

        _unitOfWork.SigningRequests.Update(signingRequest);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task PersistCompletedDocumentAsync(SigningRequest request, CancellationToken cancellationToken)
    {
        var providerRequestId = request.ProviderRequestId
            ?? throw new InvalidOperationException("The signing provider envelope has not been created.");
        var provider = _providerResolver.Resolve(request.Provider);
        var signedBytes = await provider.DownloadCompletedDocumentAsync(providerRequestId, cancellationToken);
        if (signedBytes.Length == 0)
        {
            throw new InvalidOperationException("The signature provider returned an empty signed document.");
        }

        await using var signedContent = new MemoryStream(signedBytes, writable: false);
        var storedDocument = await _documentContentStore.SaveSignedAsync(
            request.OrganizationId,
            request.DocumentId,
            request.DocumentVersionId,
            signedContent,
            "application/pdf",
            cancellationToken);
        request.RecordSignedDocument(storedDocument.Path, storedDocument.Hash);
    }

    public async Task DeclineAsync(Guid signingRequestId, Guid signerId, string? reason = null, CancellationToken cancellationToken = default)
    {
        var request = await GetRequestAsync(signingRequestId, cancellationToken, detailed: true);
        var signer = request.Signers.FirstOrDefault(s => s.SignerId == signerId)
            ?? throw new SignerNotFoundException(signerId);
        if (!request.CanDecline())
        {
            throw new InvalidSigningStateException(request.Status.ToString(), nameof(DeclineAsync));
        }

        signer.MarkAsDeclined(reason);
        request.MarkAsDeclined();
        _unitOfWork.Signers.Update(signer);
        _unitOfWork.SigningRequests.Update(request);
        await _auditService.RecordActionAsync(signingRequestId, signerId, nameof(SignatureActionType.Declined), cancellationToken: cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task CancelAsync(Guid signingRequestId, CancellationToken cancellationToken = default)
    {
        var request = await GetRequestAsync(signingRequestId, cancellationToken);
        if (!string.IsNullOrWhiteSpace(request.ProviderRequestId))
        {
            await CancelWithProviderAsync(request, cancellationToken);
        }

        request.Cancel();
        _unitOfWork.SigningRequests.Update(request);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task SignWithProviderAsync(
        SigningRequest request,
        Signer signer,
        SignRequest signRequest,
        CancellationToken cancellationToken)
    {
        var providerRequestId = request.ProviderRequestId
            ?? throw new InvalidOperationException("The signing provider envelope has not been created.");
        var transactionKey = $"{providerRequestId}:sign:{signer.SignerId:N}";
        var existingTransaction = await _unitOfWork.ProviderTransactions.GetByProviderRequestIdAsync(transactionKey, cancellationToken);
        if (existingTransaction?.Status == ProviderTransactionStatus.Success)
        {
            return;
        }

        var provider = _providerResolver.Resolve(request.Provider);
        var result = await provider.SignAsync(
            providerRequestId,
            signer.SignerId.ToString(),
            new SignatureActionRequest
            {
                SignatureBytes = signRequest.SignatureBytes,
                SignatureValue = signRequest.SignatureValue,
                SignedAt = DateTime.UtcNow,
                IpAddress = signRequest.IpAddress,
                UserAgent = signRequest.UserAgent
            },
            cancellationToken);
        if (!result.Success)
        {
            throw new InvalidOperationException(result.ErrorMessage ?? "The signature provider rejected the signing action.");
        }

        var transaction = existingTransaction ?? SigningProviderTransaction.Create(
            request.SigningRequestId,
            request.Provider,
            transactionKey,
            ProviderRequestType.Sign,
            JsonSerializer.Serialize(new { providerRequestId, SignerId = signer.SignerId }));
        transaction.RecordSuccess(JsonSerializer.Serialize(result));
        if (existingTransaction is null)
        {
            _unitOfWork.ProviderTransactions.Add(transaction);
        }
        else
        {
            _unitOfWork.ProviderTransactions.Update(transaction);
        }
    }

    private async Task CancelWithProviderAsync(SigningRequest request, CancellationToken cancellationToken)
    {
        var providerRequestId = request.ProviderRequestId!;
        var transactionKey = $"{providerRequestId}:cancel";
        var existingTransaction = await _unitOfWork.ProviderTransactions.GetByProviderRequestIdAsync(transactionKey, cancellationToken);
        if (existingTransaction?.Status == ProviderTransactionStatus.Success)
        {
            return;
        }

        var provider = _providerResolver.Resolve(request.Provider);
        await provider.CancelAsync(providerRequestId, cancellationToken);
        var transaction = existingTransaction ?? SigningProviderTransaction.Create(
            request.SigningRequestId,
            request.Provider,
            transactionKey,
            ProviderRequestType.Cancel,
            JsonSerializer.Serialize(new { providerRequestId }));
        transaction.RecordSuccess("{\"status\":\"cancelled\"}");
        if (existingTransaction is null)
        {
            _unitOfWork.ProviderTransactions.Add(transaction);
        }
        else
        {
            _unitOfWork.ProviderTransactions.Update(transaction);
        }
    }

    public async Task ResendInvitationAsync(Guid signingRequestId, Guid signerId, CancellationToken cancellationToken = default)
    {
        var request = await GetRequestAsync(signingRequestId, cancellationToken, detailed: true);
        var signer = request.Signers.FirstOrDefault(s => s.SignerId == signerId)
            ?? throw new SignerNotFoundException(signerId);
        if (!request.CanSign())
        {
            throw new InvalidSigningStateException(request.Status.ToString(), nameof(ResendInvitationAsync));
        }

        signer.MarkAsInvited();
        signer.RecordReminder();
        _unitOfWork.Signers.Update(signer);
        await _auditService.RecordActionAsync(signingRequestId, signerId, nameof(SignatureActionType.Reminded), cancellationToken: cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public Task<List<AuditEntryDto>> GetAuditTrailAsync(Guid signingRequestId, CancellationToken cancellationToken = default)
        => _auditService.GetAuditTrailAsync(signingRequestId, cancellationToken);

    public async Task ProcessExpiredRequestsAsync(CancellationToken cancellationToken = default)
    {
        var requests = await _unitOfWork.SigningRequests.FindExpiredAsync(cancellationToken);
        foreach (var request in requests)
        {
            if (!string.IsNullOrWhiteSpace(request.ProviderRequestId))
            {
                await CancelWithProviderAsync(request, cancellationToken);
            }

            request.Expire();
            SigningTelemetry.RequestsExpired.Add(1);
            _unitOfWork.SigningRequests.Update(request);
        }

        if (requests.Count > 0)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task ProcessRemindersAsync(CancellationToken cancellationToken = default)
    {
        var requests = await _unitOfWork.SigningRequests.FindByStatusAsync((int)SigningRequestStatus.Pending, 100, cancellationToken);
        foreach (var request in requests)
        {
            var signers = await _unitOfWork.Signers.GetBySigningRequestIdAsync(request.SigningRequestId, cancellationToken);
            foreach (var signer in signers.Where(s => s.Status is SignerStatus.Pending or SignerStatus.Invited))
            {
                signer.RecordReminder();
                _unitOfWork.Signers.Update(signer);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<SigningRequest> GetRequestAsync(Guid id, CancellationToken cancellationToken, bool detailed = false)
    {
        var request = detailed
            ? await _unitOfWork.SigningRequests.GetDetailedAsync(id, cancellationToken)
            : await _unitOfWork.SigningRequests.GetByIdAsync(id, cancellationToken);
        if (request is null)
        {
            throw new SigningRequestNotFoundException(id);
        }

        return request;
    }

    private static SigningRequestDto MapSummary(SigningRequest request, IEnumerable<Signer> signers)
    {
        var signerList = signers.ToList();
        return new SigningRequestDto
        {
            SigningRequestId = request.SigningRequestId,
            Title = request.Title,
            Status = request.Status,
            SigningMode = request.SigningMode,
            SignerCount = signerList.Count,
            SignedCount = signerList.Count(s => s.Status == SignerStatus.Signed),
            CreatedAt = request.CreatedAt.UtcDateTime,
            ExpiresAt = request.ExpiresAt
        };
    }

    private static SigningRequestDetailDto MapDetail(SigningRequest request, IEnumerable<SignatureField> fields, List<AuditEntryDto> auditTrail)
    {
        return new SigningRequestDetailDto
        {
            SigningRequestId = request.SigningRequestId,
            WorkflowInstanceId = request.WorkflowInstanceId,
            DocumentId = request.DocumentId,
            Title = request.Title,
            Message = request.Message,
            Status = request.Status,
            SigningMode = request.SigningMode,
            CreatedAt = request.CreatedAt.UtcDateTime,
            ExpiresAt = request.ExpiresAt,
            ActivatedAt = request.ActivatedAt,
            CompletedAt = request.CompletedAt,
            Signers = request.Signers.Select(s => new SignerDto
            {
                SignerId = s.SignerId,
                Email = s.Email,
                DisplayName = s.DisplayName,
                Role = s.Role,
                Status = s.Status,
                SigningOrder = s.SigningOrder,
                InvitedAt = s.InvitedAt,
                SignedAt = s.SignedAt,
                DeclinedAt = s.DeclinedAt,
                DeclineReason = s.DeclineReason
            }).ToList(),
            SignatureFields = fields.Select(f => new SignatureFieldDto
            {
                SignatureFieldId = f.SignatureFieldId,
                SignerId = f.SignerId,
                DocumentPage = f.DocumentPage,
                FieldType = f.FieldType,
                X = f.X,
                Y = f.Y,
                Width = f.Width,
                Height = f.Height,
                Required = f.Required,
                Label = f.Label,
                CompletedAt = f.CompletedAt
            }).ToList(),
            AuditTrail = auditTrail
        };
    }
}