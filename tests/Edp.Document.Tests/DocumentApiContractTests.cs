using Edp.Document.Api.Controllers;
using Edp.Document.Application.Interfaces;
using Edp.Document.Contracts.Requests;
using Edp.Document.Contracts.Responses;
using Edp.Document.Domain.Exceptions;
using Edp.Shared.Security.CurrentUser;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Edp.Document.Tests;

public sealed class DocumentApiContractTests
{
    [Fact]
    public async Task CreateWhenOrganizationContextMissingThrowsForbiddenProblemDetailsException()
    {
        var service = new FakeDocumentService();
        var controller = new DocumentsController(service, new CurrentOrganization { OrganizationId = null });

        var ex = await Assert.ThrowsAsync<Edp.Shared.Infrastructure.Exceptions.ForbiddenProblemDetailsException>(
            () => controller.Create(new CreateDocumentRequest { Name = "Offer", TemplateId = Guid.NewGuid() }, CancellationToken.None));

        Assert.Equal("An organization context is required to access documents.", ex.Detail);
    }

    [Fact]
    public async Task CreateWhenOrganizationContextPresentReturnsCreatedAtAction()
    {
        var service = new FakeDocumentService();
        var organizationId = Guid.NewGuid();
        var controller = new DocumentsController(service, new CurrentOrganization { OrganizationId = organizationId });

        var result = await controller.Create(new CreateDocumentRequest
        {
            Name = "Offer",
            DocumentType = "Contract",
            TemplateId = Guid.NewGuid(),
            Description = "Test"
        }, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(DocumentsController.Get), created.ActionName);
        Assert.NotNull(created.RouteValues);
        Assert.Equal(service.DocumentId, created.RouteValues!["documentId"]);
    }

    [Fact]
    public async Task GenerateWhenValidationFailsMapsToBadRequestProblemDetails()
    {
        var service = new FakeDocumentService();
        var organizationId = Guid.NewGuid();
        var controller = new DocumentsController(service, new CurrentOrganization { OrganizationId = organizationId });

        var ex = await Assert.ThrowsAsync<Edp.Shared.Infrastructure.Exceptions.ValidationProblemDetailsException>(
            () => controller.Generate(Guid.NewGuid(), new GenerateDocumentRequest
            {
                Name = "Offer",
                Data = new Dictionary<string, object?> { ["CustomerName"] = "" },
                OutputFormats = ["DOCX"]
            }, CancellationToken.None));

        Assert.Contains("CustomerName", ex.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DownloadReturnsGeneratedFile()
    {
        var service = new FakeDocumentService();
        var organizationId = Guid.NewGuid();
        var controller = new DocumentsController(service, new CurrentOrganization { OrganizationId = organizationId });

        var result = await controller.Download(Guid.NewGuid(), "PDF", CancellationToken.None);

        var file = Assert.IsType<FileStreamResult>(result);
        Assert.Equal("application/pdf", file.ContentType);
        Assert.Equal("Offer.pdf", file.FileDownloadName);
    }

    private sealed class FakeDocumentService : IDocumentService
    {
        public Guid DocumentId { get; } = Guid.NewGuid();

        public Task<DocumentSummaryResponse> CreateAsync(Guid organizationId, CreateDocumentRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(new DocumentSummaryResponse
            {
                Id = DocumentId,
                OrganizationId = organizationId,
                Name = request.Name,
                DocumentType = request.DocumentType,
                Status = "Requested",
                TemplateId = request.TemplateId,
                CurrentVersionNumber = 1,
                CreatedAt = DateTimeOffset.UtcNow
            });

        public Task<DocumentDetailResponse?> GetAsync(Guid organizationId, Guid documentId, CancellationToken cancellationToken = default)
            => Task.FromResult<DocumentDetailResponse?>(new DocumentDetailResponse
            {
                Id = documentId,
                OrganizationId = organizationId,
                Name = "Offer",
                DocumentType = "Contract",
                Status = "Requested",
                TemplateId = Guid.NewGuid(),
                CurrentVersionNumber = 1,
                CreatedAt = DateTimeOffset.UtcNow
            });

        public Task<IReadOnlyList<DocumentSummaryResponse>> ListAsync(Guid organizationId, ListDocumentsRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<DocumentSummaryResponse>>(new[]
            {
                new DocumentSummaryResponse
                {
                    Id = DocumentId,
                    OrganizationId = organizationId,
                    Name = "Offer",
                    DocumentType = "Contract",
                    Status = "Requested",
                    TemplateId = Guid.NewGuid(),
                    CurrentVersionNumber = 1,
                    CreatedAt = DateTimeOffset.UtcNow
                }
            });

        public Task<DocumentGenerationResponse> GenerateAsync(Guid organizationId, Guid documentId, GenerateDocumentRequest request, CancellationToken cancellationToken = default)
        {
            if (request.Data.TryGetValue("CustomerName", out var value) && string.IsNullOrWhiteSpace(value?.ToString()))
            {
                throw new DocumentDomainException("CustomerName is required.");
            }

            return Task.FromResult(new DocumentGenerationResponse
            {
                DocumentId = documentId,
                JobId = Guid.NewGuid(),
                Status = "Completed",
                Message = "Document generated successfully."
            });
        }

        public Task<DocumentDownload?> DownloadAsync(Guid organizationId, Guid documentId, string? fileType = null, CancellationToken cancellationToken = default)
            => Task.FromResult<DocumentDownload?>(new DocumentDownload(
                new MemoryStream(),
                string.Equals(fileType, "PDF", StringComparison.OrdinalIgnoreCase) ? "Offer.pdf" : "Offer.docx",
                string.Equals(fileType, "PDF", StringComparison.OrdinalIgnoreCase)
                    ? "application/pdf"
                    : "application/vnd.openxmlformats-officedocument.wordprocessingml.document"));
    }
}
