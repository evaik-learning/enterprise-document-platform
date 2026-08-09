using Edp.Template.Application.Dto;

namespace Edp.Template.Application.Contracts;

public interface ITemplateValidator
{
    Task<ValidationResultDto> ValidateAsync(Guid templateId, Guid versionId, CancellationToken cancellationToken = default);
}
