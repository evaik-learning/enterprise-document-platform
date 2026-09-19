namespace Edp.DigitalSignature.Infrastructure.Providers;

using Edp.DigitalSignature.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

/// <summary>
/// Demo/test implementation of ISignatureProvider for local development.
/// Simulates external signing provider behavior without actual external service calls.
/// </summary>
public class LocalDemoSignatureProvider : ISignatureProvider
{
    private readonly ILogger<LocalDemoSignatureProvider> _logger;
    private readonly Dictionary<string, EnvelopeState> _envelopes = new();

    public LocalDemoSignatureProvider(ILogger<LocalDemoSignatureProvider> logger)
    {
        _logger = logger;
    }

    public async Task<SignatureEnvelopeResult> CreateEnvelopeAsync(
        SignatureEnvelopeRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("LocalDemoProvider: Creating envelope for document '{Title}'", request.Title);

        // Generate a mock provider request ID
        var providerRequestId = string.IsNullOrWhiteSpace(request.IdempotencyKey)
            ? $"DEMO-{Guid.NewGuid():N}"
            : $"DEMO-{request.IdempotencyKey}";

        // Store envelope state
        var state = new EnvelopeState
        {
            ProviderRequestId = providerRequestId,
            Status = "pending",
            CreatedAt = DateTime.UtcNow,
            Signers = request.Signers.ToDictionary(s => s.SignerId, s => new SignerState { Status = "pending" }),
        };
        _envelopes[providerRequestId] = state;

        // Generate mock signing URLs for each signer
        var signingUrls = new Dictionary<string, string>();
        foreach (var signer in request.Signers)
        {
            signingUrls[signer.SignerId] = $"https://localhost:7001/signing/{providerRequestId}/{signer.SignerId}";
        }

        _logger.LogInformation("LocalDemoProvider: Envelope {ProviderRequestId} created successfully", providerRequestId);
        await Task.Delay(100, cancellationToken); // Simulate network delay
        
        return new SignatureEnvelopeResult
        {
            ProviderRequestId = providerRequestId,
            Status = "pending",
            SignerInvitationUrls = signingUrls,
        };
    }

    public async Task<SignatureStatusResult> GetStatusAsync(
        string providerRequestId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("LocalDemoProvider: Getting status for envelope {ProviderRequestId}", providerRequestId);

        if (!_envelopes.TryGetValue(providerRequestId, out var state))
        {
            throw new KeyNotFoundException($"Envelope {providerRequestId} not found");
        }

        var signerStatuses = state.Signers
            .Select(kvp => new SignerStatusInfo
            {
                SignerId = kvp.Key,
                Status = kvp.Value.Status,
                SignedAt = kvp.Value.SignedAt,
            })
            .ToList();

        await Task.Delay(50, cancellationToken); // Simulate network delay
        return new SignatureStatusResult
        {
            ProviderRequestId = providerRequestId,
            Status = state.Status,
            SignersStatus = signerStatuses,
        };
    }

    public async Task<SignatureActionResult> SignAsync(
        string providerRequestId,
        string signerId,
        SignatureActionRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("LocalDemoProvider: Processing signature for envelope {ProviderRequestId}, signer {SignerId}", 
            providerRequestId, signerId);

        if (!_envelopes.TryGetValue(providerRequestId, out var state))
        {
            throw new KeyNotFoundException($"Envelope {providerRequestId} not found");
        }

        if (!state.Signers.TryGetValue(signerId, out var signerState))
        {
            return new SignatureActionResult
            {
                Success = false,
                SignerId = signerId,
                SignedAt = DateTime.UtcNow,
                ErrorMessage = $"Signer {signerId} not found in envelope",
            };
        }

        // Update signer status to signed
        signerState.Status = "signed";
        signerState.SignedAt = DateTime.UtcNow;

        // Check if all signers have signed
        if (state.Signers.Values.All(s => s.Status == "signed"))
        {
            state.Status = "completed";
        }
        else
        {
            state.Status = "in_progress";
        }

        _logger.LogInformation("LocalDemoProvider: Signer {SignerId} signed envelope {ProviderRequestId}", signerId, providerRequestId);

        await Task.Delay(100, cancellationToken); // Simulate network delay
        return new SignatureActionResult
        {
            Success = true,
            SignerId = signerId,
            SignedAt = DateTime.UtcNow,
        };
    }

    public async Task CancelAsync(
        string providerRequestId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("LocalDemoProvider: Cancelling envelope {ProviderRequestId}", providerRequestId);

        if (_envelopes.TryGetValue(providerRequestId, out var state))
        {
            state.Status = "cancelled";
            _logger.LogInformation("LocalDemoProvider: Envelope {ProviderRequestId} cancelled", providerRequestId);
        }

        await Task.Delay(50, cancellationToken); // Simulate network delay
    }

    public async Task<byte[]> DownloadCompletedDocumentAsync(
        string providerRequestId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("LocalDemoProvider: Downloading signed document for envelope {ProviderRequestId}", providerRequestId);

        if (!_envelopes.TryGetValue(providerRequestId, out var state))
        {
            throw new KeyNotFoundException($"Envelope {providerRequestId} not found");
        }

        if (state.Status != "completed")
        {
            throw new InvalidOperationException($"Envelope {providerRequestId} is not completed yet (status: {state.Status})");
        }

        // Generate mock signed document bytes
        var documentBytes = System.Text.Encoding.UTF8.GetBytes(
            $"MOCK SIGNED DOCUMENT\nEnvelope: {providerRequestId}\nAll signers have signed this document.\n" +
            $"Signed at: {DateTime.UtcNow:O}\n"
        );

        _logger.LogInformation("LocalDemoProvider: Document downloaded for envelope {ProviderRequestId}, size: {Size} bytes", 
            providerRequestId, documentBytes.Length);

        await Task.Delay(150, cancellationToken); // Simulate network delay
        return documentBytes;
    }

    private class EnvelopeState
    {
        public string ProviderRequestId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public Dictionary<string, SignerState> Signers { get; set; } = new();
    }

    private class SignerState
    {
        public string Status { get; set; } = "pending";
        public DateTime? SignedAt { get; set; }
    }
}

/// <summary>
/// Factory for creating signature provider instances based on provider name.
/// </summary>
public class SignatureProviderFactory : ISignatureProviderResolver
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SignatureProviderFactory> _logger;

    public SignatureProviderFactory(IServiceProvider serviceProvider, ILogger<SignatureProviderFactory> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public ISignatureProvider CreateProvider(string providerName)
    {
        _logger.LogInformation("Creating signature provider: {ProviderName}", providerName);

        return providerName.ToLower() switch
        {
            "localdemo" => (ISignatureProvider)_serviceProvider.GetRequiredService<LocalDemoSignatureProvider>(),
            _ => throw new NotSupportedException($"Signature provider '{providerName}' is not supported"),
        };
    }

    public ISignatureProvider Resolve(string providerName) => CreateProvider(providerName);
}
