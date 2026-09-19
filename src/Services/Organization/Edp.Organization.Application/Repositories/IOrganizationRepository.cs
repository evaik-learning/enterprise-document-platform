namespace Edp.Organization.Application.Repositories;

using Edp.Organization.Domain.Entities;

public interface IOrganizationRepository
{
    Task<global::Edp.Organization.Domain.Entities.Organization> AddAsync(global::Edp.Organization.Domain.Entities.Organization organization, CancellationToken cancellationToken = default);
    Task<global::Edp.Organization.Domain.Entities.Organization?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<global::Edp.Organization.Domain.Entities.Organization>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<global::Edp.Organization.Domain.Entities.Organization>> GetForUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<OrganizationMember?> GetMemberAsync(Guid organizationId, Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OrganizationMember>> GetMembersAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task AddMemberAsync(OrganizationMember member, CancellationToken cancellationToken = default);
    Task SaveMemberAsync(OrganizationMember member, CancellationToken cancellationToken = default);
}
