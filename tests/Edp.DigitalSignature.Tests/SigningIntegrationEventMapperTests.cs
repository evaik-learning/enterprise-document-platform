namespace Edp.DigitalSignature.Tests;

using Edp.DigitalSignature.Domain.Events;
using Edp.DigitalSignature.Infrastructure.Messaging;
using Xunit;

public sealed class SigningIntegrationEventMapperTests
{
    [Fact]
    public void SignedDocumentEvent_PreservesFinalHash()
    {
        var expectedHash = "ABC123";
        var domainEvent = new SignedDocumentCreatedDomainEvent(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "organizations/org/documents/doc/versions/version/signed.pdf",
            expectedHash,
            DateTime.UtcNow,
            Guid.NewGuid());

        var mapped = SigningIntegrationEventMapper.Map(Guid.NewGuid(), domainEvent);

        Assert.NotNull(mapped);
        var integrationEvent = Assert.IsType<Edp.DigitalSignature.Contracts.Events.SignedDocumentCreatedEvent>(mapped!.Value.Data);
        Assert.Equal(expectedHash, integrationEvent.DocumentHash);
    }
}