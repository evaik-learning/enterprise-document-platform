using Edp.Shared.Infrastructure.DependencyInjection;
using Microsoft.AspNetCore.Authentication;

namespace Edp.Template.Api.Security;

public static class TemplateAuthorizationPolicies
{
    public const string TemplateRead = "Template.Read";
    public const string TemplateCreate = "Template.Create";
    public const string TemplateUpdate = "Template.Update";
    public const string TemplateUpload = "Template.Upload";
    public const string TemplateValidate = "Template.Validate";
    public const string TemplateActivate = "Template.Activate";
    public const string TemplateDeactivate = "Template.Deactivate";
    public const string TemplateArchive = "Template.Archive";

    public static IServiceCollection AddTemplateAuthorization(this IServiceCollection services)
    {
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = "Bearer";
            options.DefaultChallengeScheme = "Bearer";
        });

        services.AddSharedAuthorization(
            TemplateRead,
            TemplateCreate,
            TemplateUpdate,
            TemplateUpload,
            TemplateValidate,
            TemplateActivate,
            TemplateDeactivate,
            TemplateArchive);

        return services;
    }
}
