using System.Text.Json;
using Edp.Shared.Contracts;
using Edp.Shared.Messaging.Abstractions;
using Edp.Template.Application.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Edp.Template.Infrastructure.Messaging;

public sealed class OutboxBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxBackgroundService> _logger;
    private readonly TimeSpan _pollInterval = TimeSpan.FromSeconds(10);

    public OutboxBackgroundService(IServiceScopeFactory scopeFactory, ILogger<OutboxBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DispatchPendingAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while dispatching template outbox messages.");
            }

            await Task.Delay(_pollInterval, stoppingToken);
        }
    }

    private async Task DispatchPendingAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var outbox = scope.ServiceProvider.GetRequiredService<IOutboxMessageRepository>();
        var publisher = scope.ServiceProvider.GetRequiredService<IMessagePublisher>();

        var pending = await outbox.GetPendingAsync(20, cancellationToken);
        foreach (var message in pending)
        {
            try
            {
                var envelope = JsonSerializer.Deserialize<EventEnvelope>(message.Payload)
                    ?? throw new InvalidOperationException("Outbox message payload could not be deserialized.");

                await publisher.PublishEnvelopeAsync(envelope, cancellationToken);
                await outbox.MarkProcessedAsync(message.Id, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish outbox event {EventType} ({Id}).", message.EventType, message.Id);
                await outbox.MarkFailedAsync(message.Id, ex.Message, cancellationToken);
            }
        }
    }
}
