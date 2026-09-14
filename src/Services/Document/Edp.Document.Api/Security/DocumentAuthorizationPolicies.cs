using Edp.Shared.Infrastructure.DependencyInjection;

namespace Edp.Document.Api.Security;

public static class DocumentAuthorizationPolicies
{
    public const string DocumentRead = "Document.Read";
    public const string DocumentCreate = "Document.Create";
    public const string DocumentGenerate = "Document.Generate";

    public static IServiceCollection AddDocumentAuthorization(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSharedJwtBearerAuthentication(configuration);
        services.AddSharedAuthorization(DocumentRead, DocumentCreate, DocumentGenerate);
        return services;
    }
}