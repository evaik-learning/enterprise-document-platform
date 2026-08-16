using Edp.Template.Application.Contracts;
using Edp.Template.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Edp.Template.Infrastructure.Persistence;

public sealed class ValidationResultRepository : IValidationResultRepository
{
    private readonly TemplateDbContext _db;

    public ValidationResultRepository(TemplateDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(ValidationResultEntity result, CancellationToken cancellationToken = default)
    {
        await _db.ValidationResults.AddAsync(result, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<ValidationResultEntity?> GetLatestByVersionIdAsync(Guid templateVersionId, CancellationToken cancellationToken = default)
    {
        return await _db.ValidationResults
            .Where(v => v.TemplateVersionId == templateVersionId)
            .OrderByDescending(v => v.ValidatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
