using Edp.Organization.Application.Repositories;
using Edp.Organization.Domain.Entities;
using Edp.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Edp.Organization.Infrastructure.Repositories;

public sealed class OrganizationRepository : IOrganizationRepository
{
    private readonly EdpDbContext _dbContext;

    public OrganizationRepository(EdpDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<global::Edp.Organization.Domain.Entities.Organization> AddAsync(global::Edp.Organization.Domain.Entities.Organization organization, CancellationToken cancellationToken = default)
    {
        await _dbContext.Organizations.AddAsync(organization, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return organization;
    }

    public async Task<global::Edp.Organization.Domain.Entities.Organization?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Organizations
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<global::Edp.Organization.Domain.Entities.Organization>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Organizations
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<global::Edp.Organization.Domain.Entities.Organization>> GetForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Organizations
            .AsNoTracking()
            .Where(organization => _dbContext.OrganizationMembers.Any(member =>
                member.OrganizationId == organization.Id && member.UserId == userId && member.IsActive))
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public Task<OrganizationMember?> GetMemberAsync(Guid organizationId, Guid userId, CancellationToken cancellationToken = default) =>
        _dbContext.OrganizationMembers.FirstOrDefaultAsync(
            member => member.OrganizationId == organizationId && member.UserId == userId,
            cancellationToken);

    public Task<IReadOnlyList<OrganizationMember>> GetMembersAsync(Guid organizationId, CancellationToken cancellationToken = default) =>
        _dbContext.OrganizationMembers
            .AsNoTracking()
            .Where(member => member.OrganizationId == organizationId)
            .OrderBy(member => member.Role)
            .ThenBy(member => member.UserId)
            .ToListAsync(cancellationToken)
            .ContinueWith(task => (IReadOnlyList<OrganizationMember>)task.Result, cancellationToken);

    public async Task AddMemberAsync(OrganizationMember member, CancellationToken cancellationToken = default)
    {
        await _dbContext.OrganizationMembers.AddAsync(member, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task SaveMemberAsync(OrganizationMember member, CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
