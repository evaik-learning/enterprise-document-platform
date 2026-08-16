namespace Edp.Organization.Application.Repositories;

public interface IOrganizationRepository
{
    Task<global::Edp.Organization.Domain.Entities.Organization> AddAsync(global::Edp.Organization.Domain.Entities.Organization organization, CancellationToken cancellationToken = default);
    Task<global::Edp.Organization.Domain.Entities.Organization?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<global::Edp.Organization.Domain.Entities.Organization>> GetAllAsync(CancellationToken cancellationToken = default);
}
