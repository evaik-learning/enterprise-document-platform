namespace Edp.DigitalSignature.Tests;

using Edp.DigitalSignature.Domain.Entities;
using Edp.DigitalSignature.Domain.Enums;
using Edp.DigitalSignature.Domain.Exceptions;
using Xunit;

public sealed class SigningRequestDomainTests
{
    [Fact]
    public void Activate_TransitionsDraftRequestToPending()
    {
        var request = CreateRequest();

        request.Activate();

        Assert.Equal(SigningRequestStatus.Pending, request.Status);
        Assert.NotNull(request.ActivatedAt);
    }

    [Fact]
    public void Activate_RejectsAlreadyActivatedRequest()
    {
        var request = CreateRequest();
        request.Activate();

        var exception = Assert.Throws<InvalidSigningStateException>(() => request.Activate());

        Assert.Equal("INVALID_SIGNING_STATE", exception.ErrorCode);
    }

    [Fact]
    public void CompleteSigning_RequiresAllRequiredSignersToBeSignedByCaller()
    {
        var request = CreateRequest();
        var signer = Signer.Create(request.SigningRequestId, null, "signer@example.com", "Signer", "Approver", 1, true);
        request.Signers.Add(signer);
        request.Activate();
        request.StartSigning();

        signer.MarkAsSigned();
        request.CompleteSigning();

        Assert.Equal(SigningRequestStatus.Completed, request.Status);
        Assert.NotNull(request.CompletedAt);
    }

    [Fact]
    public void Cancel_RejectsCompletedRequest()
    {
        var request = CreateRequest();
        var signer = Signer.Create(request.SigningRequestId, null, "signer@example.com", "Signer", "Approver", 1, true);
        request.Signers.Add(signer);
        request.Activate();
        request.StartSigning();
        signer.MarkAsSigned();
        request.CompleteSigning();

        Assert.Throws<InvalidSigningStateException>(() => request.Cancel());
    }

    private static SigningRequest CreateRequest()
    {
        return SigningRequest.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            SigningMode.Sequential,
            "Test signing request",
            null,
            DateTime.UtcNow.AddDays(1),
            "hash",
            "LocalDemo",
            Guid.NewGuid());
    }
}