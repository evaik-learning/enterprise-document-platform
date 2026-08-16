using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Packaging;
using Edp.Template.Application.Contracts;
using Edp.Template.Application.Dto;

namespace Edp.Template.Infrastructure.Document;

public class OpenXmlPlaceholderExtractor : IPlaceholderExtractor
{
    public async Task<IEnumerable<PlaceholderDto>> ExtractAsync(Stream docxStream, CancellationToken cancellationToken = default)
    {
        using var ms = new MemoryStream();
        await docxStream.CopyToAsync(ms, cancellationToken);
        ms.Position = 0;

        string combinedText;
        using (var doc = WordprocessingDocument.Open(ms, false))
        {
            var parts = new List<string>();

            var bodyText = doc.MainDocumentPart?.Document?.Body?.InnerText ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(bodyText))
            {
                parts.Add(bodyText);
            }

            if (doc.MainDocumentPart?.HeaderParts is not null)
            {
                foreach (var header in doc.MainDocumentPart.HeaderParts)
                {
                    var headerText = header.Header?.InnerText ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(headerText))
                    {
                        parts.Add(headerText);
                    }
                }
            }

            if (doc.MainDocumentPart?.FooterParts is not null)
            {
                foreach (var footer in doc.MainDocumentPart.FooterParts)
                {
                    var footerText = footer.Footer?.InnerText ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(footerText))
                    {
                        parts.Add(footerText);
                    }
                }
            }

            combinedText = string.Join(' ', parts);
        }

        return PlaceholderEngine.Analyze(combinedText)
            .Select(definition => new PlaceholderDto
            {
                Name = definition.Name,
                DisplayName = definition.DisplayName,
                DataType = definition.DataType.ToString(),
                IsRequired = definition.IsRequired,
                Description = definition.Description,
                Occurrences = definition.Occurrences
            });
    }
}
