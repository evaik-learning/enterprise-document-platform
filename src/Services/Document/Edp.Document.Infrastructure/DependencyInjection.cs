using Azure.Messaging.ServiceBus;
using Edp.Document.Application.Interfaces;
using Edp.Document.Infrastructure.Background;
using Edp.Document.Infrastructure.Generation;
using Edp.Document.Infrastructure.Messaging;
using Edp.Document.Infrastructure.Persistence;
using Edp.Document.Infrastructure.Storage;
using Edp.Document.Infrastructure.Templates;
using Edp.Document.Infrastructure.Validation;
using Edp.Shared.Infrastructure.DependencyInjection;
using Edp.Shared.Messaging;
using Edp.Shared.Messaging.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Edp.Document.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddDocumentInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DocumentDb")
            ?? "Server=(localdb)\\MSSQLLocalDB;Database=DocumentDb;Trusted_Connection=True;TrustServerCertificate=True;";

        services.AddDbContext<DocumentDbContext>(options => options.UseSqlServer(connectionString));
        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<IDocumentVersionRepository, DocumentVersionRepository>();
        services.AddScoped<IDocumentFileRepository, DocumentFileRepository>();
        services.AddScoped<IDocumentGenerationJobRepository, DocumentGenerationJobRepository>();

        services.AddAzureBlobStorage(configuration.GetConnectionString("BlobStorage") ?? "UseDevelopmentStorage=true", "documents");

        var serviceBusConnectionString = configuration.GetConnectionString("ServiceBus");
        var topicName = configuration["ServiceBus:DocumentTopic"] ?? "document-events";
        if (!string.IsNullOrWhiteSpace(serviceBusConnectionString))
        {
            services.AddSingleton(new ServiceBusClient(serviceBusConnectionString));
            services.AddScoped<IMessagePublisher>(sp => new ServiceBusMessagePublisher(sp.GetRequiredService<ServiceBusClient>(), topicName));
        }
        else
        {
            services.AddScoped<IMessagePublisher, NullMessagePublisher>();
        }

        services.AddScoped<IDocumentTemplateClient, TemplateServiceClient>();
        services.AddScoped<IPlaceholderValidator, PlaceholderValidator>();
        services.AddScoped<IDocumentGenerator, OpenXmlDocumentGenerator>();
        services.AddScoped<IDocumentConverter, PdfDocumentConverter>();
        services.AddScoped<IDocumentStorage, BlobDocumentStorage>();
        services.AddScoped<IEventPublisher, DocumentEventPublisher>();
        services.AddHostedService<DocumentGenerationBackgroundService>();
        return services;
    }
}
