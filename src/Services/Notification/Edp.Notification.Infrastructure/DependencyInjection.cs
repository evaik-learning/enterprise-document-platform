using Edp.Notification.Application.Interfaces;
using Edp.Notification.Infrastructure.Messaging;
using Edp.Notification.Infrastructure.Persistence;
using Edp.Notification.Application.Services;
using Edp.Persistence;
using Edp.Shared.Infrastructure.DependencyInjection;
using Edp.Shared.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Edp.Notification.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddNotificationInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("EdpDb")
            ?? throw new InvalidOperationException("Connection string 'EdpDb' is not configured.");

        services.AddDbContext<EdpDbContext>(options => options.UseSqlServer(connectionString));
        services.AddUnitOfWork<EdpDbContext>();
        services.AddScoped<INotificationUnitOfWork, NotificationUnitOfWork>();
        services.AddScoped<INotificationInboxRepository, NotificationInboxRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddSharedServiceBusPublisher(configuration, "ServiceBus:NotificationTopic", "notification-events");
        services.AddSingleton<NotificationMessageHandler>();
        services.AddHostedService<NotificationSubscriptionHostedService>();
        return services;
    }
}
