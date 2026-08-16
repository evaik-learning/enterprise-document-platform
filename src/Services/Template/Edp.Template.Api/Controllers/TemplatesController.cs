using Edp.Shared.Security.CurrentUser;
using Edp.Template.Api.Models;
using Edp.Template.Api.Security;
using Edp.Template.Application.Commands;
using Edp.Template.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Edp.Template.Api.Controllers;

[ApiController]
[Authorize(Policy = TemplateAuthorizationPolicies.TemplateRead)]
[Route("api/v1/templates")]
public sealed class TemplatesController : ControllerBase
{
    private readonly ITemplateService _templateService;
    private readonly ICurrentUser _currentUser;
    private readonly ICurrentOrganization _currentOrganization;

    public TemplatesController(ITemplateService templateService, ICurrentUser currentUser, ICurrentOrganization currentOrganization)
    {
        _templateService = templateService;
        _currentUser = currentUser;
        _currentOrganization = currentOrganization;
    }

    [HttpPost]
    [Authorize(Policy = TemplateAuthorizationPolicies.TemplateCreate)]
    public async Task<IActionResult> Create([FromBody] CreateTemplateRequest request, CancellationToken cancellationToken)
    {
        var organizationId = RequireOrganization();
        var dto = await _templateService.CreateAsync(organizationId, CurrentUserId, new CreateTemplateCommand(request.Name, request.Code, request.Description), cancellationToken);
        return CreatedAtAction(nameof(Get), new { templateId = dto.Id }, dto);
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? search, [FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var organizationId = RequireOrganization();
        var result = await _templateService.ListAsync(organizationId, search, status, page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{templateId:guid}")]
    public async Task<IActionResult> Get(Guid templateId, CancellationToken cancellationToken)
    {
        var organizationId = RequireOrganization();
        var dto = await _templateService.GetAsync(organizationId, templateId, cancellationToken);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpPut("{templateId:guid}")]
    [Authorize(Policy = TemplateAuthorizationPolicies.TemplateUpdate)]
    public async Task<IActionResult> Update(Guid templateId, [FromBody] UpdateTemplateRequest request, CancellationToken cancellationToken)
    {
        var organizationId = RequireOrganization();
        var rowVersion = Convert.FromBase64String(request.RowVersion);
        var dto = await _templateService.UpdateAsync(organizationId, CurrentUserId, templateId, new UpdateTemplateCommand(request.Name, request.Description, rowVersion), cancellationToken);
        return Ok(dto);
    }

    [HttpPost("{templateId:guid}/versions")]
    [RequestSizeLimit(15_000_000)]
    [Authorize(Policy = TemplateAuthorizationPolicies.TemplateUpload)]
    public async Task<IActionResult> UploadVersion(Guid templateId, [FromForm] UploadTemplateVersionRequest request, CancellationToken cancellationToken)
    {
        var organizationId = RequireOrganization();

        if (request.File is null || request.File.Length == 0)
        {
            return BadRequest("A non-empty file is required.");
        }

        await using var stream = request.File.OpenReadStream();
        var dto = await _templateService.UploadVersionAsync(
            organizationId,
            CurrentUserId,
            templateId,
            stream,
            request.File.FileName,
            request.File.ContentType,
            request.File.Length,
            request.ChangeDescription,
            cancellationToken);

        return CreatedAtAction(nameof(GetVersion), new { templateId, versionId = dto.Id }, dto);
    }

    [HttpGet("{templateId:guid}/versions")]
    public async Task<IActionResult> GetVersions(Guid templateId, CancellationToken cancellationToken)
    {
        var organizationId = RequireOrganization();
        var versions = await _templateService.GetVersionsAsync(organizationId, templateId, cancellationToken);
        return Ok(versions);
    }

    [HttpGet("{templateId:guid}/versions/{versionId:guid}")]
    public async Task<IActionResult> GetVersion(Guid templateId, Guid versionId, CancellationToken cancellationToken)
    {
        var organizationId = RequireOrganization();
        var version = await _templateService.GetVersionAsync(organizationId, templateId, versionId, cancellationToken);
        return version is null ? NotFound() : Ok(version);
    }

    [HttpGet("{templateId:guid}/versions/{versionId:guid}/download")]
    public async Task<IActionResult> DownloadVersion(Guid templateId, Guid versionId, CancellationToken cancellationToken)
    {
        var organizationId = RequireOrganization();
        var download = await _templateService.DownloadVersionAsync(organizationId, templateId, versionId, cancellationToken);
        return download is null ? NotFound() : File(download.Value.Content, download.Value.ContentType, download.Value.FileName);
    }

    [HttpPost("{templateId:guid}/versions/{versionId:guid}/validate")]
    [Authorize(Policy = TemplateAuthorizationPolicies.TemplateValidate)]
    public async Task<IActionResult> ValidateVersion(Guid templateId, Guid versionId, CancellationToken cancellationToken)
    {
        var organizationId = RequireOrganization();
        var result = await _templateService.ValidateVersionAsync(organizationId, CurrentUserId, templateId, versionId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{templateId:guid}/versions/{versionId:guid}/validation")]
    public async Task<IActionResult> GetValidation(Guid templateId, Guid versionId, CancellationToken cancellationToken)
    {
        var organizationId = RequireOrganization();
        var result = await _templateService.GetValidationResultAsync(organizationId, templateId, versionId, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("{templateId:guid}/versions/{versionId:guid}/activate")]
    [Authorize(Policy = TemplateAuthorizationPolicies.TemplateActivate)]
    public async Task<IActionResult> ActivateVersion(Guid templateId, Guid versionId, CancellationToken cancellationToken)
    {
        var organizationId = RequireOrganization();
        var dto = await _templateService.ActivateVersionAsync(organizationId, CurrentUserId, templateId, versionId, cancellationToken);
        return Ok(dto);
    }

    [HttpPost("{templateId:guid}/deactivate")]
    [Authorize(Policy = TemplateAuthorizationPolicies.TemplateDeactivate)]
    public async Task<IActionResult> Deactivate(Guid templateId, CancellationToken cancellationToken)
    {
        var organizationId = RequireOrganization();
        var dto = await _templateService.DeactivateAsync(organizationId, CurrentUserId, templateId, cancellationToken);
        return Ok(dto);
    }

    [HttpPost("{templateId:guid}/archive")]
    [Authorize(Policy = TemplateAuthorizationPolicies.TemplateArchive)]
    public async Task<IActionResult> Archive(Guid templateId, CancellationToken cancellationToken)
    {
        var organizationId = RequireOrganization();
        var dto = await _templateService.ArchiveAsync(organizationId, CurrentUserId, templateId, cancellationToken);
        return Ok(dto);
    }

    private Guid? CurrentUserId => _currentUser.IsAuthenticated ? _currentUser.UserId : null;

    private Guid RequireOrganization()
    {
        return _currentOrganization.OrganizationId
            ?? throw new Edp.Shared.Infrastructure.Exceptions.ForbiddenProblemDetailsException("An organization context is required to access templates.");
    }
}
