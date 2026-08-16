namespace Edp.Shared.Storage.Abstractions;

public interface IBlobStorageService
{
    Task<string> UploadAsync(Stream content, string path, CancellationToken cancellationToken = default);
    Task<Stream?> DownloadAsync(string path, CancellationToken cancellationToken = default);
    Task DeleteAsync(string path, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string path, CancellationToken cancellationToken = default);
}
