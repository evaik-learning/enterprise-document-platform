using Azure.Messaging.ServiceBus;
using Edp.Shared.Messaging.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Edp.Shared.Messaging;

public static class DependencyInjection
{
    public static IServiceCollection AddSharedServiceBusPublisher(
        this IServiceCollection services,
        IConfiguration configuration,
        string topicSetting,
        string defaultTopic)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topicSetting);
        ArgumentException.ThrowIfNullOrWhiteSpace(defaultTopic);

        var connectionString = configuration.GetConnectionString("ServiceBus");
        var topicName = configuration[topicSetting] ?? defaultTopic;
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            services.AddScoped<IMessagePublisher, NullMessagePublisher>();
            return services;
        }

        services.AddSingleton(new ServiceBusClient(connectionString));
        services.AddSingleton<ServiceBusMessageSubscriber>();
        services.AddScoped<IMessagePublisher>(serviceProvider =>
            new ServiceBusMessagePublisher(
                serviceProvider.GetRequiredService<ServiceBusClient>(),
                topicName));

        return services;
    }
}
