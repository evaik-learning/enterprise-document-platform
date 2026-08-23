using Edp.Document.Application.Interfaces;
using Edp.Document.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Edp.Document.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddDocumentApplication(this IServiceCollection services)
    {
        services.AddScoped<IDocumentService, DocumentService>();
        services.AddScoped<IPlaceholderResolutionService, PlaceholderResolutionService>();
        services.AddScoped<IDocumentGenerationService, DocumentGenerationService>();
        return services;
    }
}
