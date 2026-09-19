using Edp.Shared.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Edp.Audit.Infrastructure.Messaging;

public sealed class AuditSubscriptionHostedService : BackgroundService
{
    private readonly ServiceBusMessageSubscriber? _subscriber;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AuditSubscriptionHostedService> _logger;

    public AuditSubscriptionHostedService(
        IServiceProvider serviceProvider,
        IServiceScopeFactory scopeFactory,
        ILogger<AuditSubscriptionHostedService> logger)
    {
        _subscriber = serviceProvider.GetService<ServiceBusMessageSubscriber>();
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_subscriber is null)
        {
            _logger.LogInformation("Service Bus is not configured; audit subscriptions are disabled.");
            await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<AuditMessageHandler>();
        await _subscriber.SubscribeAsync("template-events", "audit", handler, stoppingToken);
        await _subscriber.SubscribeAsync("document-events", "audit", handler, stoppingToken);
        await _subscriber.SubscribeAsync("workflow-events", "audit", handler, stoppingToken);
        await _subscriber.SubscribeAsync("signature-events", "audit", handler, stoppingToken);
        await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
    }
}
