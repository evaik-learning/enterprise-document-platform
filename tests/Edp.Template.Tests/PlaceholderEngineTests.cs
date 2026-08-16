using Edp.Template.Domain.Enums;
using Edp.Template.Infrastructure.Document;
using Xunit;

namespace Edp.Template.Tests;

public class PlaceholderEngineTests
{
    [Fact]
    public void AnalyzeWhenTextContainsValidAndInvalidPlaceholdersOnlyReturnsValidDefinitions()
    {
        var definitions = PlaceholderEngine.Analyze("{{CustomerName}} {{InvoiceDate}} {{Address}} {{ CustomerName }} {{Customer-Name}} {{123Customer}} {CustomerName}");

        Assert.Equal(["CustomerName", "InvoiceDate", "Address"], definitions.Select(x => x.Name).ToArray());
        Assert.All(definitions, x => Assert.True(x.IsRequired));
    }

    [Fact]
    public void AnalyzeInfersMetadataForKnownPlaceholderPatterns()
    {
        var definitions = PlaceholderEngine.Analyze("{{CustomerName}} {{InvoiceDate}} {{EmailAddress}} {{TotalAmount}} {{IsActive}}");

        Assert.Contains(definitions, x => x.Name == "CustomerName" && x.DisplayName == "Customer Name" && x.DataType == PlaceholderDataType.String);
        Assert.Contains(definitions, x => x.Name == "InvoiceDate" && x.DisplayName == "Invoice Date" && x.DataType == PlaceholderDataType.Date);
        Assert.Contains(definitions, x => x.Name == "EmailAddress" && x.DataType == PlaceholderDataType.Email);
        Assert.Contains(definitions, x => x.Name == "TotalAmount" && x.DataType == PlaceholderDataType.Decimal);
        Assert.Contains(definitions, x => x.Name == "IsActive" && x.DataType == PlaceholderDataType.Boolean);
    }
}
