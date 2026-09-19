namespace Edp.DigitalSignature.Contracts.Events;

public sealed record SigningRequestCreatedEvent(
    Guid EventId,
    Guid SigningRequestId,
    Guid WorkflowInstanceId,
    Guid DocumentId,
    Guid OrganizationId,
    string Title,
    string? Message,
    DateTimeOffset OccurredAt,
    string CorrelationId);

public sealed record SigningRequestActivatedEvent(
    Guid EventId,
    Guid SigningRequestId,
    Guid OrganizationId,
    DateTimeOffset OccurredAt,
    string CorrelationId);

public sealed record SignerInvitedEvent(
    Guid EventId,
    Guid SigningRequestId,
    Guid SignerId,
    Guid OrganizationId,
    string Email,
    string DisplayName,
    DateTimeOffset OccurredAt,
    string CorrelationId);

public sealed record SignerSignedEvent(
    Guid EventId,
    Guid SigningRequestId,
    Guid SignerId,
    Guid OrganizationId,
    DateTimeOffset SignedAt,
    string CorrelationId);

public sealed record SignerDeclinedEvent(
    Guid EventId,
    Guid SigningRequestId,
    Guid SignerId,
    Guid OrganizationId,
    string? Reason,
    DateTimeOffset DeclinedAt,
    string CorrelationId);

public sealed record SigningRequestCompletedEvent(
    Guid EventId,
    Guid SigningRequestId,
    Guid WorkflowInstanceId,
    Guid DocumentId,
    Guid OrganizationId,
    DateTimeOffset CompletedAt,
    string CorrelationId);

public sealed record SigningRequestCancelledEvent(
    Guid EventId,
    Guid SigningRequestId,
    Guid OrganizationId,
    DateTimeOffset CancelledAt,
    string CorrelationId);

public sealed record SigningRequestExpiredEvent(
    Guid EventId,
    Guid SigningRequestId,
    Guid OrganizationId,
    DateTimeOffset ExpiredAt,
    string CorrelationId);

public sealed record SignedDocumentCreatedEvent(
    Guid EventId,
    Guid SigningRequestId,
    Guid DocumentId,
    Guid OrganizationId,
    string DocumentHash,
    DateTimeOffset CreatedAt,
    string CorrelationId);