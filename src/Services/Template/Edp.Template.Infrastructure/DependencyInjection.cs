using Azure.Messaging.ServiceBus;
using Edp.Shared.Messaging;
using Edp.Shared.Messaging.Abstractions;
using Edp.Shared.Infrastructure.DependencyInjection;
using Edp.Template.Application.Common;
using Edp.Template.Application.Contracts;
using Edp.Template.Infrastructure.Audit;
using Edp.Template.Infrastructure.Document;
using Edp.Template.Infrastructure.Messaging;
using Edp.Template.Infrastructure.Outbox;
using Edp.Template.Infrastructure.Persistence;
using Edp.Template.Infrastructure.Validation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Edp.Template.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddTemplateInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("TemplateDb")
            ?? "Server=(localdb)\\MSSQLLocalDB;Database=TemplateDb;Trusted_Connection=True;TrustServerCertificate=True;";

        services.AddDbContext<TemplateDbContext>(options => options.UseSqlServer(connectionString));

        var uploadSettings = new TemplateUploadSettings
        {
            MaxFileSizeBytes = long.TryParse(configuration["TemplateService:MaxFileSizeBytes"], out var maxBytes) ? maxBytes : 10 * 1024 * 1024,
            AllowedExtensions = GetArraySetting(configuration, "TemplateService:AllowedExtensions", [".docx"]),
            AllowedContentTypes = GetArraySetting(configuration, "TemplateService:AllowedContentTypes",
                ["application/vnd.openxmlformats-officedocument.wordprocessingml.document"]),
            BlobContainer = configuration["TemplateService:BlobContainer"] ?? "templates"
        };
        services.AddSingleton(uploadSettings);

        var blobConnectionString = configuration.GetConnectionString("BlobStorage") ?? "UseDevelopmentStorage=true";
        services.AddAzureBlobStorage(blobConnectionString, uploadSettings.BlobContainer);

        var serviceBusConnectionString = configuration.GetConnectionString("ServiceBus");
        var serviceBusTopic = configuration["ServiceBus:TemplateTopic"] ?? "template-events";
        if (!string.IsNullOrWhiteSpace(serviceBusConnectionString))
        {
            services.AddSingleton(new ServiceBusClient(serviceBusConnectionString));
            services.AddScoped<IMessagePublisher>(sp => new ServiceBusMessagePublisher(sp.GetRequiredService<ServiceBusClient>(), serviceBusTopic));
        }
        else
        {
            services.AddScoped<IMessagePublisher, NullMessagePublisher>();
        }

        services.AddScoped<IOutboxMessageRepository, TemplateOutboxRepository>();
        services.AddHostedService<OutboxBackgroundService>();
        services.AddScoped<IIntegrationEventPublisher, TemplateEventPublisher>();

        services.AddHttpClient<ITemplateAuditLogger, TemplateAuditLogger>();

        services.AddScoped<ITemplateRepository, TemplateRepository>();
        services.AddScoped<ITemplateVersionRepository, TemplateVersionRepository>();
        services.AddScoped<IPlaceholderRepository, PlaceholderRepository>();
        services.AddScoped<IValidationResultRepository, ValidationResultRepository>();

        services.AddScoped<IPlaceholderExtractor, OpenXmlPlaceholderExtractor>();
        services.AddScoped<ITemplateValidator, TemplateValidator>();

        return services;
    }

    private static string[] GetArraySetting(IConfiguration configuration, string key, string[] defaultValue)
    {
        var values = configuration.GetSection(key).GetChildren()
            .Select(c => c.Value)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v => v!)
            .ToArray();
        return values.Length > 0 ? values : defaultValue;
    }
}
