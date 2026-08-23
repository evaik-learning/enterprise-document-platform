using Edp.Document.Application.Interfaces;

namespace Edp.Document.Infrastructure.Storage;

public sealed class BlobDocumentStorage : IDocumentStorage
{
    public Task<string> SaveAsync(Guid organizationId, Guid documentId, Guid versionId, string fileName, Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        var path = $"organizations/{organizationId}/documents/{documentId}/versions/{versionId}/{fileName}";
        return Task.FromResult(path);
    }

    public Task<Stream?> DownloadAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        var memory = new MemoryStream();
        using var writer = new StreamWriter(memory, leaveOpen: true);
        writer.WriteLine($"Downloaded from {storagePath}");
        writer.Flush();
        memory.Position = 0;
        return Task.FromResult<Stream?>(memory);
    }
}
