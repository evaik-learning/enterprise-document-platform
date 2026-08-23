using Edp.Document.Application.Interfaces;

namespace Edp.Document.Infrastructure.Messaging;

public sealed class DocumentEventPublisher : IEventPublisher
{
    public Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : class
    {
        return Task.CompletedTask;
    }
}
