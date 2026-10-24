using Edp.Gateway.Security;
using Edp.Gateway.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Edp.Gateway.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.GatewayAccess)]
[Route("api/v1/workflow-instances")]
public sealed class WorkflowInstancesController(WorkflowContextLoader loader) : ControllerBase
{
    [HttpGet("{instanceId:guid}")]
    public async Task<IActionResult> Get(Guid instanceId, CancellationToken cancellationToken)
    {
        var (statusCode, context) = await loader.LoadAsync(instanceId, includeHistory: true, cancellationToken);
        return context is null ? StatusCode(statusCode) : Ok(context);
    }
}
