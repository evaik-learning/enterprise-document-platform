using System.Text.Json;
using Edp.Document.Application.Contracts;
using Edp.Shared.Contracts;
using Edp.Shared.Messaging.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Edp.Document.Infrastructure.Messaging;

public sealed class DocumentOutboxBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DocumentOutboxBackgroundService> _logger;
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(10);

    public DocumentOutboxBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<DocumentOutboxBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(PollInterval);
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
                _logger.LogError(exception, "Document outbox dispatch failed.");
            }
        }
    }

    private async Task DispatchPendingAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var outbox = scope.ServiceProvider.GetRequiredService<IDocumentOutboxMessageRepository>();
        var publisher = scope.ServiceProvider.GetRequiredService<IMessagePublisher>();

        var pending = await outbox.GetPendingAsync(20, cancellationToken);
        foreach (var message in pending)
        {
            try
            {
                var envelope = JsonSerializer.Deserialize<EventEnvelope>(message.Payload)
                    ?? throw new InvalidOperationException("Document outbox payload could not be deserialized.");

                await publisher.PublishEnvelopeAsync(envelope, cancellationToken);
                await outbox.MarkProcessedAsync(message.Id, cancellationToken);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to publish document outbox event {EventType} ({Id}).", message.EventType, message.Id);
                await outbox.MarkFailedAsync(message.Id, exception.Message, cancellationToken);
            }
        }
    }
}
