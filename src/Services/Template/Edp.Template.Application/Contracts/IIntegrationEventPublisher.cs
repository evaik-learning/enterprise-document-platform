using Edp.SharedKernel.Domain;

namespace Edp.Template.Application.Contracts;

/// <summary>Publishes a domain event raised by the Template aggregate as an integration event.</summary>
public interface IIntegrationEventPublisher
{
    Task PublishAsync(DomainEvent domainEvent, CancellationToken cancellationToken = default);

    Task PublishRangeAsync(IEnumerable<DomainEvent> domainEvents, CancellationToken cancellationToken = default);
}
