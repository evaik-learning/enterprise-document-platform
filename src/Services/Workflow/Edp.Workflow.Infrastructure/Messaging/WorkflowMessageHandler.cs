using System.Text.Json;
using System.Diagnostics;
using Edp.Shared.Contracts;
using Edp.Shared.Infrastructure.Persistence;
using Edp.Shared.Messaging.Abstractions;
using Edp.Workflow.Application.Contracts;
using Edp.Workflow.Application.Interfaces;
using Edp.Workflow.Contracts.Events;
using Edp.Workflow.Infrastructure.Observability;
using Microsoft.Extensions.DependencyInjection;

namespace Edp.Workflow.Infrastructure.Messaging;

public sealed class WorkflowMessageHandler : IMessageHandler
{
    private readonly IServiceScopeFactory _scopeFactory;

    public WorkflowMessageHandler(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task HandleAsync(EventEnvelope envelope, string messageId, CancellationToken cancellationToken = default)
    {
        using var activity = WorkflowTelemetry.ActivitySource.StartActivity("workflow.inbox.process", ActivityKind.Consumer);
        activity?.SetTag("messaging.message.id", messageId);
        activity?.SetTag("messaging.event.type", envelope.EventType);
        using var scope = _scopeFactory.CreateScope();
        var inboxRepository = scope.ServiceProvider.GetRequiredService<IInboxMessageRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        if (await inboxRepository.ExistsAsync(messageId, cancellationToken))
        {
            WorkflowTelemetry.InboxDuplicates.Add(1);
            return;
        }

        await ProcessDocumentGeneratedAsync(scope.ServiceProvider, envelope, cancellationToken);
        await ProcessSigningCompletedAsync(scope.ServiceProvider, envelope, cancellationToken);

        await inboxRepository.AddAsync(
            new InboxMessage(messageId, envelope.EventType, envelope.OrganizationId),
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await inboxRepository.MarkProcessedAsync(messageId, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        WorkflowTelemetry.InboxProcessed.Add(1);
    }

    private static async Task ProcessDocumentGeneratedAsync(
        IServiceProvider serviceProvider,
        EventEnvelope envelope,
        CancellationToken cancellationToken)
    {
        if (!envelope.EventType.EndsWith("DocumentGenerated", StringComparison.OrdinalIgnoreCase)
            || envelope.Data is null)
        {
            return;
        }

        var documentEvent = JsonSerializer.Deserialize<DocumentGeneratedEvent>(
            JsonSerializer.Serialize(envelope.Data));
        if (documentEvent?.WorkflowId is not Guid workflowId)
        {
            return;
        }

        var executionService = serviceProvider.GetRequiredService<IWorkflowExecutionService>();
        await executionService.StartAsync(
            envelope.OrganizationId ?? documentEvent.OrganizationId,
            workflowId,
            documentEvent.DocumentId,
            envelope.UserId ?? Guid.Empty,
            envelope.CorrelationId?.ToString() ?? documentEvent.CorrelationId,
            cancellationToken: cancellationToken);
    }

    private static async Task ProcessSigningCompletedAsync(
        IServiceProvider serviceProvider,
        EventEnvelope envelope,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(envelope.EventType, "SigningRequestCompletedEvent", StringComparison.OrdinalIgnoreCase)
            || envelope.Data is null)
        {
            return;
        }

        var signingEvent = JsonSerializer.Deserialize<Edp.DigitalSignature.Contracts.Events.SigningRequestCompletedEvent>(
            JsonSerializer.Serialize(envelope.Data));
        if (signingEvent is null)
        {
            return;
        }

        var executionService = serviceProvider.GetRequiredService<IWorkflowExecutionService>();
        await executionService.CompleteSigningAsync(
            envelope.OrganizationId ?? signingEvent.OrganizationId,
            signingEvent.WorkflowInstanceId,
            envelope.UserId ?? Guid.Empty,
            envelope.CorrelationId?.ToString() ?? signingEvent.CorrelationId,
            cancellationToken);
    }
}
