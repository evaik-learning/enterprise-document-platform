using Edp.Gateway.Configuration;
using Edp.Gateway.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Edp.Gateway.Controllers;

[ApiController]
[Route("api/v1/gateway")]

public sealed class GatewayController : ControllerBase
{
    private readonly GatewayOptions _options;
    private readonly IWebHostEnvironment _environment;

    public GatewayController(IOptions<GatewayOptions> options, IWebHostEnvironment environment)
    {
        _options = options.Value;
        _environment = environment;
    }

    [HttpGet("info")]
    [Authorize]
    [ProducesResponseType<GatewayInfoResponse>(StatusCodes.Status200OK)]
    public ActionResult<GatewayInfoResponse> GetInfo()
    {
        var correlationId = HttpContext.Items[_options.Correlation.HeaderName]?.ToString()
            ?? HttpContext.TraceIdentifier;

        return Ok(new GatewayInfoResponse(
            _options.ServiceName,
            typeof(Program).Assembly.GetName().Version?.ToString() ?? "0.0.0",
            _environment.EnvironmentName,
            correlationId));
    }
}
