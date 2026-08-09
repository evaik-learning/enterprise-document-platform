using Edp.Template.Domain.Entities;

namespace Edp.Template.Application.Contracts;

public interface ITemplateVersionRepository
{
    Task AddAsync(TemplateVersion version, CancellationToken cancellationToken = default);
    Task<int> GetNextVersionNumberAsync(Guid templateId, CancellationToken cancellationToken = default);
    Task<IEnumerable<TemplateVersion>> GetByTemplateIdAsync(Guid templateId, CancellationToken cancellationToken = default);
    Task<TemplateVersion?> GetByIdAsync(Guid versionId, CancellationToken cancellationToken = default);
}
