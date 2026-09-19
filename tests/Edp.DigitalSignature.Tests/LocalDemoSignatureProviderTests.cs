namespace Edp.DigitalSignature.Tests;

using Edp.DigitalSignature.Application.Interfaces;
using Edp.DigitalSignature.Infrastructure.Providers;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

public sealed class LocalDemoSignatureProviderTests
{
    [Fact]
    public async Task CreateEnvelope_UsesStableIdempotencyKey()
    {
        var provider = new LocalDemoSignatureProvider(NullLogger<LocalDemoSignatureProvider>.Instance);
        var request = new SignatureEnvelopeRequest
        {
            IdempotencyKey = "request-123",
            Title = "Contract",
            DocumentBytes = [1, 2, 3],
            Signers = [new SignerEnvelopeInfo { SignerId = "signer-1", Email = "signer@example.com" }]
        };

        var result = await provider.CreateEnvelopeAsync(request);

        Assert.Equal("DEMO-request-123", result.ProviderRequestId);
    }

    [Fact]
    public async Task CompletedEnvelope_ReturnsSignedDocument()
    {
        var provider = new LocalDemoSignatureProvider(NullLogger<LocalDemoSignatureProvider>.Instance);
        var envelope = await provider.CreateEnvelopeAsync(new SignatureEnvelopeRequest
        {
            IdempotencyKey = "request-456",
            Title = "Contract",
            Signers = [new SignerEnvelopeInfo { SignerId = "signer-1", Email = "signer@example.com" }]
        });

        var result = await provider.SignAsync(
            envelope.ProviderRequestId,
            "signer-1",
            new SignatureActionRequest { SignatureValue = "signature", SignedAt = DateTime.UtcNow });
        var document = await provider.DownloadCompletedDocumentAsync(envelope.ProviderRequestId);

        Assert.True(result.Success);
        Assert.NotEmpty(document);
    }
}