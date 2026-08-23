using Edp.Document.Application.Interfaces;

namespace Edp.Document.Infrastructure.Generation;

public sealed class OpenXmlDocumentGenerator : IDocumentGenerator
{
    public Task<Stream> GenerateAsync(Guid organizationId, Guid templateId, Dictionary<string, object?> data, CancellationToken cancellationToken = default)
    {
        var stream = new MemoryStream();
        using var writer = new StreamWriter(stream, leaveOpen: true);
        writer.WriteLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
        writer.WriteLine("<document>");
        writer.WriteLine("  <templateId>" + templateId + "</templateId>");
        writer.WriteLine("  <organizationId>" + organizationId + "</organizationId>");
        writer.WriteLine("  <data>");
        foreach (var pair in data)
        {
            writer.WriteLine($"    <item key=\"{pair.Key}\">{pair.Value ?? string.Empty}</item>");
        }

        writer.WriteLine("  </data>");
        writer.WriteLine("</document>");
        writer.Flush();
        stream.Position = 0;
        return Task.FromResult<Stream>(stream);
    }
}
