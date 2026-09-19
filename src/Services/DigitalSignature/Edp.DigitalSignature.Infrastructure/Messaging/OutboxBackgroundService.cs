namespace Edp.DigitalSignature.Infrastructure.Messaging;

using System.Text.Json;
using Edp.DigitalSignature.Infrastructure.Outbox;
using Edp.Shared.Messaging.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public sealed class OutboxBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxBackgroundService> _logger;

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
                using var scope = _scopeFactory.CreateScope();
                var outbox = scope.ServiceProvider.GetRequiredService<SigningOutboxRepository>();
                var publisher = scope.ServiceProvider.GetRequiredService<IMessagePublisher>();
                var pending = await outbox.GetPendingAsync(20, stoppingToken);

                foreach (var message in pending)
                {
                    try
                    {
                        var envelope = JsonSerializer.Deserialize<Edp.Shared.Contracts.EventEnvelope>(message.Payload)
                            ?? throw new InvalidOperationException("Outbox payload could not be deserialized.");
                        await publisher.PublishEnvelopeAsync(envelope, stoppingToken);
                        await outbox.MarkProcessedAsync(message.Id, stoppingToken);
                    }
                    catch (Exception exception)
                    {
                        _logger.LogError(exception, "Failed to publish signing event {EventType} ({MessageId}).", message.EventType, message.Id);
                        await outbox.MarkFailedAsync(message.Id, exception.Message, stoppingToken);
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error while dispatching signing outbox messages.");
            }

            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }
}