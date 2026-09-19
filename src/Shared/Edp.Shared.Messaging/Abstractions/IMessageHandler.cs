using Edp.Shared.Contracts;

namespace Edp.Shared.Messaging.Abstractions;

public interface IMessageHandler
{
    Task HandleAsync(EventEnvelope envelope, string messageId, CancellationToken cancellationToken = default);
}
