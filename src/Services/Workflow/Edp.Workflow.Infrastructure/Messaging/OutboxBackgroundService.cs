using System.Text.Json;
using Edp.Shared.Contracts;
using Edp.Shared.Messaging.Abstractions;
using Edp.Workflow.Application.Contracts;
using Edp.Shared.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Edp.Workflow.Infrastructure.Messaging;

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
        using var timer = new PeriodicTimer(_pollInterval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await DispatchPendingAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Workflow outbox dispatch failed.");
            }
        }
    }

    private async Task DispatchPendingAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var outbox = scope.ServiceProvider.GetRequiredService<IOutboxMessageRepository>();
        var publisher = scope.ServiceProvider.GetRequiredService<IMessagePublisher>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var pending = await outbox.GetPendingAsync(20, cancellationToken);
        foreach (var message in pending)
        {
            try
            {
                var envelope = JsonSerializer.Deserialize<EventEnvelope>(message.Payload)
                    ?? throw new InvalidOperationException("Workflow outbox payload could not be deserialized.");
                await publisher.PublishEnvelopeAsync(envelope, cancellationToken);
                await outbox.MarkProcessedAsync(message.Id, cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to publish Workflow outbox event {EventType} ({Id}).", message.EventType, message.Id);
                await outbox.MarkFailedAsync(message.Id, exception.Message, cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
