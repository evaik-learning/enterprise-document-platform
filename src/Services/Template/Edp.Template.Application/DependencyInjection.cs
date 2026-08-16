using Edp.Template.Application.Interfaces;
using Edp.Template.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Edp.Template.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddTemplateApplication(this IServiceCollection services)
    {
        services.AddScoped<ITemplateService, TemplateService>();
        return services;
    }
}
