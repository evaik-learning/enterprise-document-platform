namespace Edp.Shared.Contracts;

public sealed class EventEnvelope
{
    public Guid EventId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid? OrganizationId { get; set; }
    public Guid? UserId { get; set; }
    public Guid? CorrelationId { get; set; }
    public int Version { get; set; } = 1;
    public object? Data { get; set; }
}
