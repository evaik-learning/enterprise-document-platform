using Edp.Organization.Application.Commands;
using Edp.Organization.Application.Interfaces;
using Edp.Organization.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Edp.Organization.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public sealed class OrganizationsController : ControllerBase
{
    private readonly IOrganizationService _organizationService;

    public OrganizationsController(IOrganizationService organizationService)
    {
        _organizationService = organizationService;
    }

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
