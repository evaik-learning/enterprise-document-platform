using Edp.Document.Application.Interfaces;

namespace Edp.Document.Infrastructure.Templates;

public sealed class TemplateServiceClient : IDocumentTemplateClient
{
    public Task<object?> GetTemplateAsync(Guid organizationId, Guid templateId, CancellationToken cancellationToken = default)
    {
        var templateMetadata = new
        {
            organizationId,
            templateId,
            name = "Customer Contract",
            status = "active",
            version = 1,
            isPublished = true
        };

        return Task.FromResult<object?>(templateMetadata);
    }
}
