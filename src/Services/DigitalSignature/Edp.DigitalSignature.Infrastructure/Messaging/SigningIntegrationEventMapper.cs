namespace Edp.DigitalSignature.Infrastructure.Messaging;

using Edp.DigitalSignature.Contracts.Events;
using Edp.DigitalSignature.Domain.Events;
using Edp.SharedKernel.Domain;

public static class SigningIntegrationEventMapper
{
    public static (string EventType, object Data)? Map(Guid eventId, DomainEvent domainEvent)
    {
        return domainEvent switch
        {
            SigningRequestCreatedDomainEvent value =>
                (nameof(SigningRequestCreatedEvent), new SigningRequestCreatedEvent(
                    eventId, value.SigningRequestId, value.WorkflowInstanceId, value.DocumentId,
                    value.OrganizationId, value.Title, value.Message, value.CreatedAt,
                    value.CorrelationId.ToString())),
            SigningRequestActivatedDomainEvent value =>
                (nameof(SigningRequestActivatedEvent), new SigningRequestActivatedEvent(
                    eventId, value.SigningRequestId, value.OrganizationId, value.ActivatedAt,
                    value.CorrelationId.ToString())),
            SignerInvitedDomainEvent value =>
                (nameof(SignerInvitedEvent), new SignerInvitedEvent(
                    eventId, value.SigningRequestId, value.SignerId, value.OrganizationId,
                    value.Email, value.DisplayName, value.InvitedAt, value.CorrelationId.ToString())),
            SignerSignedDomainEvent value =>
                (nameof(SignerSignedEvent), new SignerSignedEvent(
                    eventId, value.SigningRequestId, value.SignerId, value.OrganizationId,
                    value.SignedAt, value.CorrelationId.ToString())),
            SignerDeclinedDomainEvent value =>
                (nameof(SignerDeclinedEvent), new SignerDeclinedEvent(
                    eventId, value.SigningRequestId, value.SignerId, value.OrganizationId,
                    value.Reason, value.DeclinedAt, value.CorrelationId.ToString())),
            SigningRequestCompletedDomainEvent value =>
                (nameof(SigningRequestCompletedEvent), new SigningRequestCompletedEvent(
                    eventId, value.SigningRequestId, value.WorkflowInstanceId, value.DocumentId,
                    value.OrganizationId, value.CompletedAt, value.CorrelationId.ToString())),
            SigningRequestCancelledDomainEvent value =>
                (nameof(SigningRequestCancelledEvent), new SigningRequestCancelledEvent(
                    eventId, value.SigningRequestId, value.OrganizationId, value.CancelledAt,
                    value.CorrelationId.ToString())),
            SigningRequestExpiredDomainEvent value =>
                (nameof(SigningRequestExpiredEvent), new SigningRequestExpiredEvent(
                    eventId, value.SigningRequestId, value.OrganizationId, value.ExpiredAt,
                    value.CorrelationId.ToString())),
            SignedDocumentCreatedDomainEvent value =>
                (nameof(SignedDocumentCreatedEvent), new SignedDocumentCreatedEvent(
                    eventId, value.SigningRequestId, value.DocumentId, value.OrganizationId,
                    value.DocumentHash, value.CreatedAt, value.CorrelationId.ToString())),
            _ => null
        };
    }
}