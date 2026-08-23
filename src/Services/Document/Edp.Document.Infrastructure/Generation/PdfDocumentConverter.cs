using Edp.Document.Application.Interfaces;

namespace Edp.Document.Infrastructure.Generation;

public sealed class PdfDocumentConverter : IDocumentConverter
{
    public Task<Stream> ConvertAsync(Stream source, string sourceFormat, string targetFormat, CancellationToken cancellationToken = default)
    {
        if (sourceFormat.Equals("docx", StringComparison.OrdinalIgnoreCase) && targetFormat.Equals("pdf", StringComparison.OrdinalIgnoreCase))
        {
            var bytes = new byte[source.Length];
            source.Position = 0;
            _ = source.Read(bytes, 0, bytes.Length);
            var pdfStream = new MemoryStream(bytes);
            return Task.FromResult<Stream>(pdfStream);
        }

        source.Position = 0;
        return Task.FromResult<Stream>(source);
    }
}
