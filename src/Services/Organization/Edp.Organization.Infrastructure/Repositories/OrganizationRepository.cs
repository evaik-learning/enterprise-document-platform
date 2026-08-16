using Edp.Organization.Application.Repositories;
using Edp.Organization.Domain.Entities;
using Edp.Organization.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Edp.Organization.Infrastructure.Repositories;

public sealed class OrganizationRepository : IOrganizationRepository
{
    private readonly OrganizationDbContext _dbContext;

    public OrganizationRepository(OrganizationDbContext dbContext)
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
}
