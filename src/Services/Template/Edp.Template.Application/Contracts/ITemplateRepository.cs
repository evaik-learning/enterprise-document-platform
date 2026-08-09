using Edp.Template.Domain.Entities;

namespace Edp.Template.Application.Contracts;

public interface ITemplateRepository
{
    Task<global::Edp.Template.Domain.Entities.Template?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(global::Edp.Template.Domain.Entities.Template template, CancellationToken cancellationToken = default);
    Task<(IEnumerable<global::Edp.Template.Domain.Entities.Template> Items, int TotalCount)> SearchAsync(
        string? name = null,
        string? status = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);
}
