using Edp.Template.Domain.Entities;

namespace Edp.Template.Application.Contracts;

public interface ITemplateVersionRepository
{
    Task AddAsync(TemplateVersion version, CancellationToken cancellationToken = default);

    Task UpdateAsync(TemplateVersion version, CancellationToken cancellationToken = default);

    Task<int> GetNextVersionNumberAsync(Guid templateId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TemplateVersion>> GetByTemplateIdAsync(Guid organizationId, Guid templateId, CancellationToken cancellationToken = default);

    Task<TemplateVersion?> GetByIdAsync(Guid organizationId, Guid templateId, Guid versionId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TemplateVersion>> GetActiveVersionsAsync(Guid templateId, CancellationToken cancellationToken = default);
}
