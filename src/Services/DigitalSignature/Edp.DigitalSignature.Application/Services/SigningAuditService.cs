namespace Edp.DigitalSignature.Application.Services;

using Edp.DigitalSignature.Application.Contracts;
using Edp.DigitalSignature.Application.Interfaces;
using Edp.DigitalSignature.Domain.Entities;
using Edp.DigitalSignature.Domain.Enums;

public sealed class SigningAuditService : ISigningAuditService
{
    private readonly IUnitOfWork _unitOfWork;

    public SigningAuditService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task RecordActionAsync(Guid signingRequestId, Guid signerId, string actionType, string? ipAddress = null, string? userAgent = null, CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<SignatureActionType>(actionType, true, out var parsedAction))
        {
            throw new ArgumentException($"Unknown signature action '{actionType}'.", nameof(actionType));
        }

        _unitOfWork.SignatureActions.Add(SignatureAction.Create(signingRequestId, signerId, parsedAction, ipAddress, userAgent));
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<AuditEntryDto>> GetAuditTrailAsync(Guid signingRequestId, CancellationToken cancellationToken = default)
    {
        var actions = await _unitOfWork.SignatureActions.GetBySigningRequestIdAsync(signingRequestId, cancellationToken);
        return await MapAsync(actions, cancellationToken);
    }

    public async Task<List<AuditEntryDto>> GetSignerAuditTrailAsync(Guid signingRequestId, Guid signerId, CancellationToken cancellationToken = default)
    {
        var actions = await _unitOfWork.SignatureActions.GetBySignerIdAsync(signerId, cancellationToken);
        return await MapAsync(actions.Where(a => a.SigningRequestId == signingRequestId), cancellationToken);
    }

    private async Task<List<AuditEntryDto>> MapAsync(IEnumerable<SignatureAction> actions, CancellationToken cancellationToken)
    {
        var result = new List<AuditEntryDto>();
        foreach (var action in actions)
        {
            var signer = await _unitOfWork.Signers.GetByIdAsync(action.SignerId, cancellationToken);
            result.Add(new AuditEntryDto
            {
                SignatureActionId = action.SignatureActionId,
                SignerId = action.SignerId,
                SignerEmail = signer?.Email ?? string.Empty,
                ActionType = action.ActionType.ToString(),
                OccurredAt = action.OccurredAt,
                IpAddress = action.IpAddress,
                UserAgent = action.UserAgent
            });
        }

        return result;
    }
}