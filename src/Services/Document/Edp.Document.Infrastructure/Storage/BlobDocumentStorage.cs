using Edp.Document.Application.Interfaces;
using Edp.Shared.Storage.Abstractions;

namespace Edp.Document.Infrastructure.Storage;

public sealed class BlobDocumentStorage : IDocumentStorage
{
    private readonly IBlobStorageService _blobStorage;

    public BlobDocumentStorage(IBlobStorageService blobStorage)
    {
        _blobStorage = blobStorage;
    }

    public Task<string> SaveAsync(Guid organizationId, Guid documentId, Guid versionId, string fileName, Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        var path = $"organizations/{organizationId}/documents/{documentId}/versions/{versionId}/{fileName}";
        if (content.CanSeek)
        {
            content.Position = 0;
        }

        return SaveCoreAsync(path, content, contentType, cancellationToken);
    }

    public Task<Stream?> DownloadAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        return _blobStorage.DownloadAsync(storagePath, cancellationToken);
    }

    private async Task<string> SaveCoreAsync(string path, Stream content, string contentType, CancellationToken cancellationToken)
    {
        await _blobStorage.UploadAsync(content, path, contentType, cancellationToken);
        return path;
    }
}
