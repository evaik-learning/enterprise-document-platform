using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Packaging;
using Edp.Template.Application.Contracts;
using Edp.Template.Application.Dto;

namespace Edp.Template.Infrastructure.Document;

public class OpenXmlPlaceholderExtractor : IPlaceholderExtractor
{
    private static readonly Regex PlaceholderRegex = new("\\{\\{([A-Za-z][A-Za-z0-9_]*)\\}\\}", RegexOptions.Compiled);

    public async Task<IEnumerable<PlaceholderDto>> ExtractAsync(Stream docxStream, CancellationToken cancellationToken = default)
    {
        // DocumentFormat.OpenXml requires a seekable stream
        using var ms = new MemoryStream();
        await docxStream.CopyToAsync(ms, cancellationToken);
        ms.Position = 0;

        var counts = new Dictionary<string, int>(StringComparer.Ordinal);

        using (var doc = WordprocessingDocument.Open(ms, false))
        {
            // Main document body
            var bodyText = doc.MainDocumentPart?.Document?.Body?.InnerText ?? string.Empty;
            ScanText(bodyText, counts);

            // Headers
            if (doc.MainDocumentPart?.HeaderParts != null)
            {
                foreach (var header in doc.MainDocumentPart.HeaderParts)
                {
                    ScanText(header.Header?.InnerText ?? string.Empty, counts);
                }
            }

            // Footers
            if (doc.MainDocumentPart?.FooterParts != null)
            {
                foreach (var footer in doc.MainDocumentPart.FooterParts)
                {
                    ScanText(footer.Footer?.InnerText ?? string.Empty, counts);
                }
            }
        }

        return counts.Select(kv => new PlaceholderDto { Name = kv.Key, Occurrences = kv.Value });
    }

    private static void ScanText(string text, Dictionary<string, int> counts)
    {
        foreach (Match m in PlaceholderRegex.Matches(text))
        {
            var name = m.Groups[1].Value;
            if (counts.ContainsKey(name)) counts[name]++; else counts[name] = 1;
        }
    }
}
