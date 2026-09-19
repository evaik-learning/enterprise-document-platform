using Edp.Document.Application.Interfaces;
using Edp.Document.Api.Security;
using Edp.Document.Contracts.Requests;
using Edp.Document.Domain.Exceptions;
using Edp.Shared.Infrastructure.Exceptions;
using Edp.Shared.Security.CurrentUser;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Edp.Document.Api.Controllers;

[ApiController]
[Authorize(Policy = DocumentAuthorizationPolicies.DocumentRead)]
[Route("api/v1/documents")]
public sealed class DocumentsController : ControllerBase
{
    private readonly IDocumentService _documentService;
    private readonly ICurrentOrganization _currentOrganization;

    public DocumentsController(IDocumentService documentService, ICurrentOrganization currentOrganization)
    {
        _documentService = documentService;
        _currentOrganization = currentOrganization;
    }

    [HttpPost]
    [Authorize(Policy = DocumentAuthorizationPolicies.DocumentCreate)]
    public async Task<IActionResult> Create([FromBody] CreateDocumentRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var organizationId = RequireOrganization();
            var document = await _documentService.CreateAsync(organizationId, request, cancellationToken);
            return CreatedAtAction(nameof(Get), new { documentId = document.Id }, document);
        }
        catch (DocumentDomainException ex)
        {
            throw MapDocumentException(ex);
        }
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? status, [FromQuery] string? templateId, [FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken cancellationToken = default)
    {
        var organizationId = RequireOrganization();
        var request = new ListDocumentsRequest { Status = status, TemplateId = templateId, Page = page, PageSize = pageSize };
        var documents = await _documentService.ListAsync(organizationId, request, cancellationToken);
        return Ok(documents);
    }

    [HttpGet("{documentId:guid}")]
    public async Task<IActionResult> Get(Guid documentId, CancellationToken cancellationToken)
    {
        var organizationId = RequireOrganization();
        var document = await _documentService.GetAsync(organizationId, documentId, cancellationToken);
        return document is null ? NotFound() : Ok(document);
    }

    [HttpGet("{documentId:guid}/download")]
    public async Task<IActionResult> Download(Guid documentId, [FromQuery] string? fileType, CancellationToken cancellationToken)
    {
        var organizationId = RequireOrganization();
        var download = await _documentService.DownloadAsync(organizationId, documentId, fileType, cancellationToken);
        return download is null
            ? NotFound()
            : File(download.Content, download.ContentType, download.FileName);
    }

    [HttpPost("{documentId:guid}/generate")]
    [Authorize(Policy = DocumentAuthorizationPolicies.DocumentGenerate)]
    public async Task<IActionResult> Generate(Guid documentId, [FromBody] GenerateDocumentRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var organizationId = RequireOrganization();
            var result = await _documentService.GenerateAsync(organizationId, documentId, request, cancellationToken);
            return Ok(result);
        }
        catch (DocumentDomainException ex)
        {
            throw MapDocumentException(ex);
        }
    }

    private static ProblemDetailsException MapDocumentException(DocumentDomainException exception)
    {
        var message = exception.Message ?? string.Empty;

        if (message.Contains("not found", StringComparison.OrdinalIgnoreCase))
        {
            return new NotFoundProblemDetailsException(message, "DOCUMENT_NOT_FOUND");
        }

        if (message.Contains("required", StringComparison.OrdinalIgnoreCase)
            || message.Contains("validation", StringComparison.OrdinalIgnoreCase)
            || message.Contains("missing", StringComparison.OrdinalIgnoreCase)
            || message.Contains("invalid", StringComparison.OrdinalIgnoreCase))
        {
            return new ValidationProblemDetailsException(message, "DOCUMENT_VALIDATION_FAILED");
        }

        if (message.Contains("template unavailable", StringComparison.OrdinalIgnoreCase)
            || message.Contains("could not be generated", StringComparison.OrdinalIgnoreCase)
            || message.Contains("failed", StringComparison.OrdinalIgnoreCase))
        {
            return new UnprocessableEntityProblemDetailsException(message, "DOCUMENT_GENERATION_FAILED");
        }

        return new ValidationProblemDetailsException(message, "DOCUMENT_REQUEST_FAILED");
    }

    private Guid RequireOrganization()
    {
        return _currentOrganization.OrganizationId
            ?? throw new ForbiddenProblemDetailsException("An organization context is required to access documents.", "DOCUMENT_ORGANIZATION_REQUIRED");
    }
}
