using Edp.Organization.Application.Commands;
using Edp.Organization.Application.Interfaces;
using Edp.Organization.Application.Models;
using Edp.Organization.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Edp.Shared.Security.CurrentUser;

namespace Edp.Organization.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/[controller]")]
public sealed class OrganizationsController : ControllerBase
{
    private readonly IOrganizationService _organizationService;
    private readonly ICurrentUser _currentUser;

    public OrganizationsController(IOrganizationService organizationService, ICurrentUser currentUser)
    {
        _organizationService = organizationService;
        _currentUser = currentUser;
    }

    [HttpGet("mine")]
    public async Task<ActionResult<IReadOnlyList<OrganizationMembershipDto>>> GetMine(CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == Guid.Empty)
            return Unauthorized();
        return Ok(await _organizationService.GetMembershipsForUserAsync(_currentUser.UserId, cancellationToken));
    }

    [HttpGet("{id:guid}/members")]
    public async Task<ActionResult<IReadOnlyList<OrganizationMemberDto>>> GetMembers(Guid id, CancellationToken cancellationToken) =>
        Ok(await _organizationService.GetMembersAsync(id, _currentUser.UserId, cancellationToken));

    [HttpPost("{id:guid}/members")]
    public async Task<ActionResult<OrganizationMemberDto>> AddMember(Guid id, AddOrganizationMemberCommand command, CancellationToken cancellationToken) =>
        Ok(await _organizationService.AddMemberAsync(id, _currentUser.UserId, command, cancellationToken));

    [HttpPut("{id:guid}/members/{userId:guid}/role")]
    public async Task<ActionResult<OrganizationMemberDto>> UpdateMemberRole(Guid id, Guid userId, UpdateOrganizationMemberRoleCommand command, CancellationToken cancellationToken) =>
        Ok(await _organizationService.UpdateMemberRoleAsync(id, _currentUser.UserId, userId, command, cancellationToken));

    [HttpDelete("{id:guid}/members/{userId:guid}")]
    public async Task<IActionResult> RemoveMember(Guid id, Guid userId, CancellationToken cancellationToken)
    {
        await _organizationService.RemoveMemberAsync(id, _currentUser.UserId, userId, cancellationToken);
        return NoContent();
    }

    [HttpGet("{id:guid}/capabilities")]
    public async Task<ActionResult<OrganizationCapabilityDto>> GetCapabilities(Guid id, CancellationToken cancellationToken) =>
        Ok(await _organizationService.GetCapabilitiesAsync(id, _currentUser.UserId, cancellationToken));

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<global::Edp.Organization.Domain.Entities.Organization>>> GetAll(CancellationToken cancellationToken)
    {
        var organizations = await _organizationService.GetAllAsync(cancellationToken);
        return Ok(organizations);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<global::Edp.Organization.Domain.Entities.Organization>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var organization = await _organizationService.GetByIdAsync(id, cancellationToken);
        return organization is null ? NotFound() : Ok(organization);
    }

    [HttpPost]
    public async Task<ActionResult<global::Edp.Organization.Domain.Entities.Organization>> Create([FromBody] CreateOrganizationCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Name))
        {
            return BadRequest("Name is required.");
        }

        var organization = await _organizationService.CreateAsync(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = organization.Id }, organization);
    }
}
