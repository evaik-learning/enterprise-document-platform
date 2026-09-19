using Edp.Organization.Application.Commands;
using Edp.Organization.Application.Models;

namespace Edp.Organization.Application.Interfaces;

public interface IOrganizationService
{
    Task<global::Edp.Organization.Domain.Entities.Organization> CreateAsync(CreateOrganizationCommand command, CancellationToken cancellationToken = default);
    Task<global::Edp.Organization.Domain.Entities.Organization?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<global::Edp.Organization.Domain.Entities.Organization>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<global::Edp.Organization.Domain.Entities.Organization>> GetForUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OrganizationMembershipDto>> GetMembershipsForUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OrganizationMemberDto>> GetMembersAsync(Guid organizationId, Guid requestingUserId, CancellationToken cancellationToken = default);
    Task<OrganizationMemberDto> AddMemberAsync(Guid organizationId, Guid requestingUserId, AddOrganizationMemberCommand command, CancellationToken cancellationToken = default);
    Task<OrganizationMemberDto> UpdateMemberRoleAsync(Guid organizationId, Guid requestingUserId, Guid userId, UpdateOrganizationMemberRoleCommand command, CancellationToken cancellationToken = default);
    Task RemoveMemberAsync(Guid organizationId, Guid requestingUserId, Guid userId, CancellationToken cancellationToken = default);
    Task<OrganizationCapabilityDto> GetCapabilitiesAsync(Guid organizationId, Guid userId, CancellationToken cancellationToken = default);
}
