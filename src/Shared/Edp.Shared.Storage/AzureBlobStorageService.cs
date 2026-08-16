using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Edp.Shared.Storage.Abstractions;

namespace Edp.Shared.Storage;

public sealed class AzureBlobStorageService : IBlobStorageService
{
    private readonly BlobServiceClient _client;
    private readonly string _containerName;

    public AzureBlobStorageService(BlobServiceClient client, string containerName = "documents")
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _containerName = string.IsNullOrWhiteSpace(containerName) ? "documents" : containerName;
    }

    public Task<string> UploadAsync(Stream content, string path, CancellationToken cancellationToken = default)
    {
        return UploadAsync(content, path, null, cancellationToken);
    }

    public async Task<string> UploadAsync(Stream content, string path, string? contentType = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Blob path is required.", nameof(path));
        }

        var container = _client.GetBlobContainerClient(_containerName);
        await container.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: cancellationToken);

        var blob = container.GetBlobClient(path);
        var options = new BlobUploadOptions
        {
            HttpHeaders = string.IsNullOrWhiteSpace(contentType)
                ? null
                : new BlobHttpHeaders { ContentType = contentType }
        };
        await blob.UploadAsync(content, options, cancellationToken);

        return blob.Uri.ToString();
    }

    public async Task<Stream?> DownloadAsync(string path, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Blob path is required.", nameof(path));
        }

        var container = _client.GetBlobContainerClient(_containerName);
        var blob = container.GetBlobClient(path);
        var exists = await blob.ExistsAsync(cancellationToken);
        if (!exists.Value)
        {
            return null;
        }

        var response = await blob.DownloadStreamingAsync(cancellationToken: cancellationToken);
        return response.Value.Content;
    }

    public async Task DeleteAsync(string path, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Blob path is required.", nameof(path));
        }

        var container = _client.GetBlobContainerClient(_containerName);
        var blob = container.GetBlobClient(path);
        await blob.DeleteIfExistsAsync(cancellationToken: cancellationToken);
    }

    public async Task<bool> ExistsAsync(string path, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Blob path is required.", nameof(path));
        }

        var container = _client.GetBlobContainerClient(_containerName);
        var blob = container.GetBlobClient(path);
        var exists = await blob.ExistsAsync(cancellationToken);
        return exists.Value;
    }
}
