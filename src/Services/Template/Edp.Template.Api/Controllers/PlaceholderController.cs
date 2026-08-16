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
[Route("api/v1/templates/{templateId:guid}/versions/{versionId:guid}/placeholders")]
public sealed class PlaceholderController : ControllerBase
{
    private readonly ITemplateService _templateService;
    private readonly ICurrentUser _currentUser;
    private readonly ICurrentOrganization _currentOrganization;

    public PlaceholderController(ITemplateService templateService, ICurrentUser currentUser, ICurrentOrganization currentOrganization)
    {
        _templateService = templateService;
        _currentUser = currentUser;
        _currentOrganization = currentOrganization;
    }

    [HttpPost]
    [Authorize(Policy = TemplateAuthorizationPolicies.TemplateUpdate)]
    public async Task<IActionResult> Create(Guid templateId, Guid versionId, [FromBody] CreatePlaceholderRequest request, CancellationToken cancellationToken)
    {
        var organizationId = RequireOrganization();
        var dto = await _templateService.CreatePlaceholderAsync(
            organizationId,
            CurrentUserId,
            templateId,
            versionId,
            new CreatePlaceholderCommand(
                request.Name,
                request.DisplayName,
                request.DataType,
                request.IsRequired,
                request.DefaultValue,
                request.Format,
                request.Description,
                request.Occurrences),
            cancellationToken);

        return CreatedAtAction(nameof(Get), new { templateId, versionId, placeholderId = dto.Id }, dto);
    }

    [HttpGet]
    public async Task<IActionResult> List(Guid templateId, Guid versionId, CancellationToken cancellationToken)
    {
        var organizationId = RequireOrganization();
        var placeholders = await _templateService.GetPlaceholdersAsync(organizationId, templateId, versionId, cancellationToken);
        return Ok(placeholders);
    }

    [HttpGet("{placeholderId:guid}")]
    public async Task<IActionResult> Get(Guid templateId, Guid versionId, Guid placeholderId, CancellationToken cancellationToken)
    {
        var organizationId = RequireOrganization();
        var placeholder = await _templateService.GetPlaceholderAsync(organizationId, templateId, versionId, placeholderId, cancellationToken);
        return placeholder is null ? NotFound() : Ok(placeholder);
    }

    [HttpPost("discover")]
    [Authorize(Policy = TemplateAuthorizationPolicies.TemplateUpdate)]
    public async Task<IActionResult> Discover(Guid templateId, Guid versionId, CancellationToken cancellationToken)
    {
        var organizationId = RequireOrganization();
        var result = await _templateService.DiscoverPlaceholdersAsync(organizationId, CurrentUserId, templateId, versionId, cancellationToken);
        return Ok(result);
    }

    [HttpPost("validate")]
    [Authorize(Policy = TemplateAuthorizationPolicies.TemplateValidate)]
    public async Task<IActionResult> ValidatePlaceholders(Guid templateId, Guid versionId, CancellationToken cancellationToken)
    {
        var organizationId = RequireOrganization();
        var result = await _templateService.ValidatePlaceholdersAsync(organizationId, CurrentUserId, templateId, versionId, cancellationToken);
        return Ok(result);
    }

    [HttpPut("{placeholderId:guid}")]
    [Authorize(Policy = TemplateAuthorizationPolicies.TemplateUpdate)]
    public async Task<IActionResult> Update(Guid templateId, Guid versionId, Guid placeholderId, [FromBody] UpdatePlaceholderRequest request, CancellationToken cancellationToken)
    {
        var organizationId = RequireOrganization();
        var dto = await _templateService.UpdatePlaceholderAsync(
            organizationId,
            CurrentUserId,
            templateId,
            versionId,
            placeholderId,
            new UpdatePlaceholderCommand(
                request.DisplayName,
                request.DataType,
                request.IsRequired,
                request.DefaultValue,
                request.Format,
                request.Description),
            cancellationToken);

        return Ok(dto);
    }

    [HttpDelete("{placeholderId:guid}")]
    [Authorize(Policy = TemplateAuthorizationPolicies.TemplateUpdate)]
    public async Task<IActionResult> Delete(Guid templateId, Guid versionId, Guid placeholderId, CancellationToken cancellationToken)
    {
        var organizationId = RequireOrganization();
        var deleted = await _templateService.DeletePlaceholderAsync(organizationId, CurrentUserId, templateId, versionId, placeholderId, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    private Guid? CurrentUserId => _currentUser.IsAuthenticated ? _currentUser.UserId : null;

    private Guid RequireOrganization()
    {
        return _currentOrganization.OrganizationId
            ?? throw new Edp.Shared.Infrastructure.Exceptions.ForbiddenProblemDetailsException("An organization context is required to access templates.");
    }
}
