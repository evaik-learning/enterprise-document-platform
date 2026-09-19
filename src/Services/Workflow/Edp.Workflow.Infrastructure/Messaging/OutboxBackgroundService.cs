using System.Text.Json;
using System.Diagnostics;
using Edp.Shared.Contracts;
using Edp.Shared.Messaging.Abstractions;
using Edp.Workflow.Application.Contracts;
using Edp.Shared.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Edp.Workflow.Application;
using Edp.Workflow.Infrastructure.Observability;

namespace Edp.Workflow.Infrastructure.Messaging;

public sealed class OutboxBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxBackgroundService> _logger;
    private readonly WorkflowOptions _options;

    public OutboxBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<OutboxBackgroundService> logger,
        IOptions<WorkflowOptions> options)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(Math.Max(1, _options.OutboxPollIntervalSeconds)));
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

        var pending = await outbox.GetPendingAsync(
            Math.Max(1, _options.OutboxBatchSize),
            Math.Max(1, _options.MaxOutboxRetryAttempts),
            cancellationToken);
        foreach (var message in pending)
        {
            using var activity = WorkflowTelemetry.ActivitySource.StartActivity("workflow.outbox.publish", ActivityKind.Producer);
            activity?.SetTag("messaging.message.id", message.Id);
            activity?.SetTag("messaging.event.type", message.EventType);
            try
            {
                var envelope = JsonSerializer.Deserialize<EventEnvelope>(message.Payload)
                    ?? throw new InvalidOperationException("Workflow outbox payload could not be deserialized.");
                await publisher.PublishEnvelopeAsync(envelope, cancellationToken);
                await outbox.MarkProcessedAsync(message.Id, cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                WorkflowTelemetry.OutboxPublished.Add(1);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to publish Workflow outbox event {EventType} ({Id}).", message.EventType, message.Id);
                await outbox.MarkFailedAsync(message.Id, exception.Message, cancellationToken);
                if (message.RetryCount >= Math.Max(1, _options.MaxOutboxRetryAttempts))
                {
                    await outbox.MarkDeadLetterAsync(message.Id, exception.Message, cancellationToken);
                    WorkflowTelemetry.OutboxDeadLettered.Add(1);
                }
                await unitOfWork.SaveChangesAsync(cancellationToken);
                WorkflowTelemetry.OutboxFailed.Add(1);
            }
        }
    }
}
