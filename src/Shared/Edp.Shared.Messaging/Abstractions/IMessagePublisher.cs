using Edp.Shared.Contracts;

namespace Edp.Shared.Messaging.Abstractions;

public interface IMessagePublisher
{
    Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default)
        where T : class;

    Task PublishEnvelopeAsync(EventEnvelope envelope, CancellationToken cancellationToken = default);
}
