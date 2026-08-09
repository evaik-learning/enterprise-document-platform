using Edp.Template.Domain.Entities;

namespace Edp.Template.Application.Contracts;

public interface IPlaceholderRepository
{
    Task AddRangeAsync(IEnumerable<Placeholder> placeholders, CancellationToken cancellationToken = default);
    Task<IEnumerable<Placeholder>> GetByVersionIdAsync(Guid versionId, CancellationToken cancellationToken = default);
}
