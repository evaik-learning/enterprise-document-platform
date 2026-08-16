using Edp.Template.Domain.Entities;

namespace Edp.Template.Application.Contracts;

public interface IValidationResultRepository
{
    Task AddAsync(ValidationResultEntity result, CancellationToken cancellationToken = default);

    Task<ValidationResultEntity?> GetLatestByVersionIdAsync(Guid templateVersionId, CancellationToken cancellationToken = default);
}
