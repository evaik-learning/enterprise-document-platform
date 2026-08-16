using Edp.Template.Domain.Entities;

namespace Edp.Template.Application.Contracts;

public interface IPlaceholderRepository
{
    Task AddAsync(Placeholder placeholder, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<Placeholder> placeholders, CancellationToken cancellationToken = default);
    Task<Placeholder?> GetByIdAsync(Guid placeholderId, CancellationToken cancellationToken = default);
    Task<Placeholder?> GetByVersionAndIdAsync(Guid versionId, Guid placeholderId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Placeholder>> GetByVersionIdAsync(Guid versionId, CancellationToken cancellationToken = default);
    Task UpdateAsync(Placeholder placeholder, CancellationToken cancellationToken = default);
    Task DeleteAsync(Placeholder placeholder, CancellationToken cancellationToken = default);
}
