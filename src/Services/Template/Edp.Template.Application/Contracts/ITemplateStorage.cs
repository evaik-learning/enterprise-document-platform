using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Edp.Template.Application.Contracts;

public interface ITemplateStorage
{
    Task<string> UploadAsync(Stream content, string path, string contentType, CancellationToken cancellationToken = default);
    Task<Stream?> DownloadAsync(string path, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string path, CancellationToken cancellationToken = default);
    Task DeleteAsync(string path, CancellationToken cancellationToken = default);
}
