namespace Edp.DigitalSignature.Infrastructure.Storage;

using System.Security.Cryptography;
using Edp.DigitalSignature.Application.Interfaces;
using Edp.Shared.Storage.Abstractions;

public sealed class SignedDocumentStorage : IDocumentContentStore
{
    private readonly IBlobStorageService _blobStorage;

    public SignedDocumentStorage(IBlobStorageService blobStorage)
    {
        _blobStorage = blobStorage;
    }

    public async Task<(string Path, string Hash)> SaveOriginalAsync(
        Guid organizationId,
        Guid documentId,
        Guid versionId,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default)
        => await SaveAsync(BuildPath(organizationId, documentId, versionId, "original"), content, contentType, cancellationToken);

    public async Task<(string Path, string Hash)> SaveSignedAsync(
        Guid organizationId,
        Guid documentId,
        Guid versionId,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default)
        => await SaveAsync(BuildPath(organizationId, documentId, versionId, "signed"), content, contentType, cancellationToken);

    public Task<Stream?> DownloadAsync(string path, CancellationToken cancellationToken = default)
        => _blobStorage.DownloadAsync(path, cancellationToken);

    public Task<Stream?> DownloadOriginalAsync(
        Guid organizationId,
        Guid documentId,
        Guid documentVersionId,
        CancellationToken cancellationToken = default)
        => DownloadAsync(BuildPath(organizationId, documentId, documentVersionId, "original"), cancellationToken);

    private async Task<(string Path, string Hash)> SaveAsync(
        string path,
        Stream content,
        string contentType,
        CancellationToken cancellationToken)
    {
        if (content.CanSeek)
        {
            content.Position = 0;
        }

        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        var bytes = buffer.ToArray();
        var hash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        await using var upload = new MemoryStream(bytes, writable: false);
        await _blobStorage.UploadAsync(upload, path, contentType, cancellationToken);
        return (path, hash);
    }

    private static string BuildPath(Guid organizationId, Guid documentId, Guid versionId, string kind)
        => $"organizations/{organizationId}/documents/{documentId}/versions/{versionId}/{kind}.pdf";
}