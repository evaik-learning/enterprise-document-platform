using Edp.Template.Application.Dto;

namespace Edp.Template.Application.Contracts;

public interface IPlaceholderExtractor
{
    Task<IEnumerable<PlaceholderDto>> ExtractAsync(Stream docxStream, CancellationToken cancellationToken = default);
}
