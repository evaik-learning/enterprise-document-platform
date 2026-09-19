using Edp.Shared.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Edp.Notification.Infrastructure.Messaging;

public sealed class NotificationSubscriptionHostedService : BackgroundService
{
    private readonly ServiceBusMessageSubscriber? _subscriber;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<NotificationSubscriptionHostedService> _logger;

    public NotificationSubscriptionHostedService(
        IServiceProvider serviceProvider,
        IServiceScopeFactory scopeFactory,
        ILogger<NotificationSubscriptionHostedService> logger)
    {
        _subscriber = serviceProvider.GetService<ServiceBusMessageSubscriber>();
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_subscriber is null)
        {
            _logger.LogInformation("Service Bus is not configured; notification subscriptions are disabled.");
            await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<NotificationMessageHandler>();
        await _subscriber.SubscribeAsync("template-events", "notification", handler, stoppingToken);
        await _subscriber.SubscribeAsync("document-events", "notification", handler, stoppingToken);
        await _subscriber.SubscribeAsync("workflow-events", "notification", handler, stoppingToken);
        await _subscriber.SubscribeAsync("signature-events", "notification", handler, stoppingToken);
        await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
    }
}
