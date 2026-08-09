namespace Edp.Shared.Messaging.Abstractions;

public interface IMessagePublisher
{
    Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default);
}
