using Edp.Organization.Application.Commands;

namespace Edp.Organization.Application.Interfaces;

public interface IOrganizationService
{
    Task<global::Edp.Organization.Domain.Entities.Organization> CreateAsync(CreateOrganizationCommand command, CancellationToken cancellationToken = default);
    Task<global::Edp.Organization.Domain.Entities.Organization?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<global::Edp.Organization.Domain.Entities.Organization>> GetAllAsync(CancellationToken cancellationToken = default);
}
