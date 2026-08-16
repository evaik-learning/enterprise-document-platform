using Edp.Template.Domain.Entities;

namespace Edp.Template.Application.Contracts;

public interface ITemplateRepository
{
    Task<global::Edp.Template.Domain.Entities.Template?> GetByIdAsync(Guid organizationId, Guid id, CancellationToken cancellationToken = default);

    Task<bool> CodeExistsAsync(Guid organizationId, string code, CancellationToken cancellationToken = default);

    Task AddAsync(global::Edp.Template.Domain.Entities.Template templateEntity, CancellationToken cancellationToken = default);

    Task UpdateAsync(global::Edp.Template.Domain.Entities.Template templateEntity, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<global::Edp.Template.Domain.Entities.Template> Items, int TotalCount)> SearchAsync(
        Guid organizationId,
        string? search = null,
        string? status = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);
}
