namespace Edp.Shared.Contracts.Dto;

public sealed class IntegrationEventDto
{
    public Guid EventId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; }
}
