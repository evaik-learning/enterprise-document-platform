using Edp.Document.Application.Interfaces;
using System.IO.Compression;
using System.Security;

namespace Edp.Document.Infrastructure.Generation;

public sealed class OpenXmlDocumentGenerator : IDocumentGenerator
{
    public Task<Stream> GenerateAsync(Guid organizationId, Guid templateId, Dictionary<string, object?> data, CancellationToken cancellationToken = default)
    {
        var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            WriteEntry(archive, "[Content_Types].xml", "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>\n<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/><Default Extension=\"xml\" ContentType=\"application/xml\"/><Override PartName=\"/word/document.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml\"/></Types>");
            WriteEntry(archive, "_rels/.rels", "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>\n<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"word/document.xml\"/></Relationships>");

            var text = $"Generated document\nOrganization: {organizationId}\nTemplate: {templateId}";
            foreach (var pair in data)
            {
                text += $"\n{pair.Key}: {pair.Value ?? string.Empty}";
            }

            var paragraphs = string.Join(string.Empty, text.Split('\n').Select(line => $"<w:p><w:r><w:t xml:space=\"preserve\">{SecurityElement.Escape(line)}</w:t></w:r></w:p>"));
            WriteEntry(archive, "word/document.xml", $"<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>\n<w:document xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:body>{paragraphs}<w:sectPr/></w:body></w:document>");
        }

        stream.Position = 0;
        return Task.FromResult<Stream>(stream);
    }

    private static void WriteEntry(ZipArchive archive, string name, string content)
    {
        using var writer = new StreamWriter(archive.CreateEntry(name).Open());
        writer.Write(content);
    }
}
