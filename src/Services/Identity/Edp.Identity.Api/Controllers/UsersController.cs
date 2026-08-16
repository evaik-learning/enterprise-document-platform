using Edp.Identity.Application.Commands;
using Edp.Identity.Application.Interfaces;
using Edp.Identity.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Edp.Identity.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class UsersController : ControllerBase
{
    private readonly IIdentityService _identityService;

    public UsersController(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    [HttpGet("by-email")]
    public async Task<ActionResult<User>> GetByEmail([FromQuery] string email, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return BadRequest("Email is required.");
        }

        var user = await _identityService.GetByEmailAsync(email, cancellationToken);
        return user is null ? NotFound() : Ok(user);
    }

    [HttpPost("register")]
    public async Task<ActionResult<User>> Register([FromBody] RegisterUserCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Email))
        {
            return BadRequest("Email is required.");
        }

        try
        {
            var user = await _identityService.RegisterAsync(command, cancellationToken);
            return CreatedAtAction(nameof(GetByEmail), new { email = user.Email }, user);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }
}
