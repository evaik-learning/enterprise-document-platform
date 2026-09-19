using Edp.Shared.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Edp.Workflow.Infrastructure.Messaging;

public sealed class WorkflowSubscriptionHostedService : BackgroundService
{
    private readonly ServiceBusMessageSubscriber? _subscriber;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<WorkflowSubscriptionHostedService> _logger;

    public WorkflowSubscriptionHostedService(
        IServiceProvider serviceProvider,
        IServiceScopeFactory scopeFactory,
        ILogger<WorkflowSubscriptionHostedService> logger)
    {
        _subscriber = serviceProvider.GetService<ServiceBusMessageSubscriber>();
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_subscriber is null)
        {
            _logger.LogInformation("Service Bus is not configured; workflow subscriptions are disabled.");
            await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<WorkflowMessageHandler>();
        await _subscriber.SubscribeAsync("document-events", "workflow", handler, stoppingToken);
        await _subscriber.SubscribeAsync("signing-events", "workflow", handler, stoppingToken);
        await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
    }
}
