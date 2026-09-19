using Edp.Organization.Application.Commands;
using Edp.Organization.Application.Interfaces;
using Edp.Organization.Application.Models;
using Edp.Organization.Application.Repositories;
using Edp.Organization.Domain.Entities;
using Edp.Shared.Infrastructure.Exceptions;

namespace Edp.Organization.Application.Services;

public sealed class OrganizationService : IOrganizationService
{
    private readonly IOrganizationRepository _repository;

    public OrganizationService(IOrganizationRepository repository)
    {
        _repository = repository;
    }

    public async Task<global::Edp.Organization.Domain.Entities.Organization> CreateAsync(CreateOrganizationCommand command, CancellationToken cancellationToken = default)
    {
        var organization = global::Edp.Organization.Domain.Entities.Organization.Create(Guid.NewGuid(), command.Name, command.Description);
        return await _repository.AddAsync(organization, cancellationToken);
    }

    public Task<global::Edp.Organization.Domain.Entities.Organization?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _repository.GetByIdAsync(id, cancellationToken);
    }

    public Task<IReadOnlyList<global::Edp.Organization.Domain.Entities.Organization>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return _repository.GetAllAsync(cancellationToken);
    }

    public Task<IReadOnlyList<global::Edp.Organization.Domain.Entities.Organization>> GetForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return _repository.GetForUserAsync(userId, cancellationToken);
    }

    public async Task<IReadOnlyList<OrganizationMembershipDto>> GetMembershipsForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var organizations = await _repository.GetForUserAsync(userId, cancellationToken);
        var memberships = new List<OrganizationMembershipDto>(organizations.Count);
        foreach (var organization in organizations)
        {
            var member = await _repository.GetMemberAsync(organization.Id, userId, cancellationToken);
            if (member is not null)
                memberships.Add(new(organization.Id, organization.Name, organization.Description, member.Role, member.IsActive));
        }
        return memberships;
    }

    public async Task<IReadOnlyList<OrganizationMemberDto>> GetMembersAsync(Guid organizationId, Guid requestingUserId, CancellationToken cancellationToken = default)
    {
        await RequireAdministratorAsync(organizationId, requestingUserId, cancellationToken);
        var members = await _repository.GetMembersAsync(organizationId, cancellationToken);
        return members.Select(ToDto).ToArray();
    }

    public async Task<OrganizationMemberDto> AddMemberAsync(Guid organizationId, Guid requestingUserId, AddOrganizationMemberCommand command, CancellationToken cancellationToken = default)
    {
        await RequireAdministratorAsync(organizationId, requestingUserId, cancellationToken);
        ValidateRole(command.Role);
        if (command.UserId == Guid.Empty) throw new ValidationProblemDetailsException("A user is required.", "ORGANIZATION_USER_REQUIRED");
        if (await _repository.GetMemberAsync(organizationId, command.UserId, cancellationToken) is not null)
            throw new ConflictProblemDetailsException("The user is already a member of this organization.", "ORGANIZATION_MEMBER_EXISTS");

        var member = OrganizationMember.Create(Guid.NewGuid(), organizationId, command.UserId, command.Role);
        await _repository.AddMemberAsync(member, cancellationToken);
        return ToDto(member);
    }

    public async Task<OrganizationMemberDto> UpdateMemberRoleAsync(Guid organizationId, Guid requestingUserId, Guid userId, UpdateOrganizationMemberRoleCommand command, CancellationToken cancellationToken = default)
    {
        await RequireAdministratorAsync(organizationId, requestingUserId, cancellationToken);
        ValidateRole(command.Role);
        var member = await RequireMemberAsync(organizationId, userId, cancellationToken);
        if (string.Equals(member.Role, OrganizationRoles.Owner, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(command.Role, OrganizationRoles.Owner, StringComparison.OrdinalIgnoreCase))
            await EnsureAnotherOwnerAsync(organizationId, userId, cancellationToken);
        member.UpdateRole(command.Role);
        await _repository.SaveMemberAsync(member, cancellationToken);
        return ToDto(member);
    }

    public async Task RemoveMemberAsync(Guid organizationId, Guid requestingUserId, Guid userId, CancellationToken cancellationToken = default)
    {
        await RequireAdministratorAsync(organizationId, requestingUserId, cancellationToken);
        if (requestingUserId == userId)
            throw new ValidationProblemDetailsException("You cannot remove your own membership.", "ORGANIZATION_SELF_REMOVE_FORBIDDEN");
        var member = await RequireMemberAsync(organizationId, userId, cancellationToken);
        if (string.Equals(member.Role, OrganizationRoles.Owner, StringComparison.OrdinalIgnoreCase))
            await EnsureAnotherOwnerAsync(organizationId, userId, cancellationToken);
        member.Deactivate();
        await _repository.SaveMemberAsync(member, cancellationToken);
    }

    public async Task<OrganizationCapabilityDto> GetCapabilitiesAsync(Guid organizationId, Guid userId, CancellationToken cancellationToken = default)
    {
        var member = await RequireMemberAsync(organizationId, userId, cancellationToken);
        return new(organizationId, userId, member.Role, PermissionsFor(member.Role));
    }

    private async Task<OrganizationMember> RequireAdministratorAsync(Guid organizationId, Guid userId, CancellationToken cancellationToken)
    {
        var member = await RequireMemberAsync(organizationId, userId, cancellationToken);
        if (!OrganizationRoles.IsAdministrator(member.Role))
            throw new ForbiddenProblemDetailsException("Organization administrator permission is required.", "ORGANIZATION_ADMIN_REQUIRED");
        return member;
    }

    private async Task<OrganizationMember> RequireMemberAsync(Guid organizationId, Guid userId, CancellationToken cancellationToken)
    {
        var member = await _repository.GetMemberAsync(organizationId, userId, cancellationToken);
        if (member is null || !member.IsActive)
            throw new ForbiddenProblemDetailsException("The user is not an active member of this organization.", "ORGANIZATION_MEMBERSHIP_REQUIRED");
        return member;
    }

    private async Task EnsureAnotherOwnerAsync(Guid organizationId, Guid excludedUserId, CancellationToken cancellationToken)
    {
        var owners = (await _repository.GetMembersAsync(organizationId, cancellationToken))
            .Count(member => member.IsActive
                && member.UserId != excludedUserId
                && string.Equals(member.Role, OrganizationRoles.Owner, StringComparison.OrdinalIgnoreCase));
        if (owners == 0)
            throw new ConflictProblemDetailsException("The organization must retain an active Owner.", "ORGANIZATION_OWNER_REQUIRED");
    }

    private static void ValidateRole(string role)
    {
        if (!new[] { OrganizationRoles.Owner, OrganizationRoles.Administrator, OrganizationRoles.Member, OrganizationRoles.Auditor }
            .Any(allowed => string.Equals(allowed, role, StringComparison.OrdinalIgnoreCase)))
            throw new ValidationProblemDetailsException("The selected organization role is not supported.", "ORGANIZATION_ROLE_INVALID");
    }

    private static OrganizationMemberDto ToDto(OrganizationMember member) => new(member.UserId, member.Role, member.IsActive);

    private static IReadOnlyList<string> PermissionsFor(string role) => role.ToLowerInvariant() switch
    {
        "owner" => PermissionCatalog.All,
        "administrator" => PermissionCatalog.Administrator,
        "auditor" => PermissionCatalog.Auditor,
        _ => PermissionCatalog.Member
    };

    private static class PermissionCatalog
    {
        public static readonly IReadOnlyList<string> All = ["Organization.Manage", "Members.Manage", "Templates.Read", "Templates.Write", "Documents.Read", "Documents.Write", "Workflow.Read", "Workflow.Start", "Approval.Approve", "Signing.Read", "Signing.Manage", "Notifications.Read", "Audit.Read"];
        public static readonly IReadOnlyList<string> Administrator = All.Where(permission => permission != "Organization.Manage").ToArray();
        public static readonly IReadOnlyList<string> Auditor = ["Organization.Read", "Templates.Read", "Documents.Read", "Workflow.Read", "Signing.Read", "Notifications.Read", "Audit.Read"];
        public static readonly IReadOnlyList<string> Member = ["Organization.Read", "Templates.Read", "Documents.Read", "Documents.Write", "Workflow.Read", "Notifications.Read"];
    }
}
