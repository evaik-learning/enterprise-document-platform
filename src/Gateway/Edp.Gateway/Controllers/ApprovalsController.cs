using Edp.Gateway.Security;
using Edp.Gateway.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Edp.Gateway.Controllers;

/// <summary>Approval inbox enriched with document and workflow context (read-only composition).</summary>
[ApiController]
[Authorize(Policy = AuthorizationPolicies.GatewayAccess)]
[Route("api/v1/approvals")]
public sealed class ApprovalsController(IDownstreamApiClient downstream) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var tasksResponse = await downstream.GetAsync(DownstreamServices.Workflow, $"/api/v1/workflows/approval-tasks/my?page={page}&pageSize={pageSize}", cancellationToken);
        if (!tasksResponse.IsSuccess) return StatusCode(tasksResponse.StatusCode);

        var tasks = tasksResponse.Body.Items();
        var instanceIds = tasks.Select(task => task.GetGuid("workflowInstanceId")).Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToList();
        var instances = await Task.WhenAll(instanceIds.Select(async id =>
            (id, response: await downstream.GetAsync(DownstreamServices.Workflow, $"/api/v1/workflows/instances/{id}", cancellationToken))));
        var instanceById = instances.Where(x => x.response.IsSuccess).ToDictionary(x => x.id, x => x.response.Body!.Value);

        var documentIds = instanceById.Values.Select(instance => instance.GetGuid("documentId")).Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToList();
        var documents = await Task.WhenAll(documentIds.Select(async id =>
            (id, response: await downstream.GetAsync(DownstreamServices.Document, $"/api/v1/documents/{id}", cancellationToken))));
        var documentById = documents.Where(x => x.response.IsSuccess).ToDictionary(x => x.id, x => x.response.Body!.Value);

        var workflows = await downstream.GetAsync(DownstreamServices.Workflow, "/api/v1/workflows?page=1&pageSize=100", cancellationToken);
        var workflowNames = new Dictionary<Guid, string?>();
        if (workflows.IsSuccess)
        {
            foreach (var workflow in workflows.Body.Items())
            {
                if (workflow.GetGuid("id") is { } id) workflowNames[id] = workflow.GetString("name");
            }
        }

        var items = tasks.Select(task =>
        {
            var instanceId = task.GetGuid("workflowInstanceId");
            System.Text.Json.JsonElement? instance = instanceId.HasValue && instanceById.TryGetValue(instanceId.Value, out var i) ? i : null;
            var documentId = instance?.GetGuid("documentId");
            System.Text.Json.JsonElement? document = documentId.HasValue && documentById.TryGetValue(documentId.Value, out var d) ? d : null;
            var workflowId = instance?.GetGuid("workflowId");
            return new
            {
                taskId = task.GetGuid("id"),
                workflowInstanceId = instanceId,
                workflowId,
                workflowName = workflowId.HasValue && workflowNames.TryGetValue(workflowId.Value, out var name) ? name : null,
                documentId,
                documentName = document?.GetString("name"),
                status = task.GetString("status"),
                instanceStatus = instance?.GetString("status"),
                assignedToUserId = task.GetGuid("assignedToUserId"),
                deadlineAt = task.GetString("deadlineAt")
            };
        });

        return Ok(new { items, page, pageSize, hasNextPage = tasks.Count == pageSize });
    }

    [HttpGet("{taskId:guid}")]
    public async Task<IActionResult> Get(Guid taskId, CancellationToken cancellationToken)
    {
        var taskResponse = await downstream.GetAsync(DownstreamServices.Workflow, $"/api/v1/workflows/approval-tasks/{taskId}", cancellationToken);
        if (!taskResponse.IsSuccess) return StatusCode(taskResponse.StatusCode);
        var task = taskResponse.Body!.Value;

        var instanceId = task.GetGuid("workflowInstanceId");
        var instanceResponse = instanceId.HasValue
            ? await downstream.GetAsync(DownstreamServices.Workflow, $"/api/v1/workflows/instances/{instanceId}", cancellationToken)
            : null;
        var instance = instanceResponse is { IsSuccess: true } ? instanceResponse.Body : null;
        var documentId = instance?.GetGuid("documentId");
        var documentTask = documentId.HasValue
            ? downstream.GetAsync(DownstreamServices.Document, $"/api/v1/documents/{documentId}", cancellationToken)
            : Task.FromResult<DownstreamResponse?>(null)!;
        var historyTask = instanceId.HasValue
            ? downstream.GetAsync(DownstreamServices.Workflow, $"/api/v1/workflows/instances/{instanceId}/history?page=1&pageSize=100", cancellationToken)
            : Task.FromResult<DownstreamResponse?>(null)!;
        await Task.WhenAll(documentTask, historyTask);
        var document = documentTask.Result is { IsSuccess: true } d ? d.Body : null;
        var history = historyTask.Result is { IsSuccess: true } h ? h.Body.Items() : [];

        return Ok(new { task, instance, document, history });
    }
}
