using System.IO;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Edp.Template.Application.Contracts;

namespace Edp.Template.Infrastructure.Storage;

public class BlobTemplateStorage : ITemplateStorage
{
    private readonly BlobServiceClient _client;
    private readonly string _container = "templates";

    public BlobTemplateStorage(BlobServiceClient client)
    {
        _client = client;
    }

    public async Task DeleteAsync(string path, CancellationToken cancellationToken = default)
    {
        var container = _client.GetBlobContainerClient(_container);
        var blob = container.GetBlobClient(path);
        await blob.DeleteIfExistsAsync(cancellationToken: cancellationToken);
    }

    public async Task<Stream?> DownloadAsync(string path, CancellationToken cancellationToken = default)
    {
        var container = _client.GetBlobContainerClient(_container);
        var blob = container.GetBlobClient(path);
        var exists = await blob.ExistsAsync(cancellationToken);
        if (!exists.Value) return null;
        var resp = await blob.DownloadAsync(cancellationToken);
        return resp.Value.Content;
    }

    public async Task<bool> ExistsAsync(string path, CancellationToken cancellationToken = default)
    {
        var container = _client.GetBlobContainerClient(_container);
        var blob = container.GetBlobClient(path);
        var exists = await blob.ExistsAsync(cancellationToken);
        return exists.Value;
    }

    public async Task<string> UploadAsync(Stream content, string path, string contentType, CancellationToken cancellationToken = default)
    {
        var container = _client.GetBlobContainerClient(_container);
        await container.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: cancellationToken);
        var blob = container.GetBlobClient(path);
        await blob.UploadAsync(content, new BlobHttpHeaders { ContentType = contentType }, cancellationToken: cancellationToken);
        return blob.Uri.ToString();
    }
}
