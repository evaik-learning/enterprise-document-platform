using System.Text.Json;
using Edp.Gateway.Security;
using Edp.Gateway.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Edp.Gateway.Controllers;

/// <summary>Cross-service document views: workspace, unified timeline and audit.</summary>
[ApiController]
[Authorize(Policy = AuthorizationPolicies.GatewayAccess)]
[Route("api/v1/documents/{documentId:guid}")]
public sealed class DocumentWorkspaceController(IDownstreamApiClient downstream, WorkflowContextLoader workflowLoader) : ControllerBase
{
    private sealed record Sources(
        JsonElement Document,
        IReadOnlyList<JsonElement> Instances,
        IReadOnlyList<JsonElement> SigningRequests,
        IReadOnlyList<string> Unavailable);

    [HttpGet("workspace")]
    public async Task<IActionResult> Workspace(Guid documentId, CancellationToken cancellationToken)
    {
        var (status, sources) = await LoadSourcesAsync(documentId, cancellationToken);
        if (sources is null) return StatusCode(status);

        var permissions = Permissions();
        var timelineTask = TimelineAsync(sources, cancellationToken);
        var latestInstanceId = sources.Instances.Select(i => i.GetGuid("id")).FirstOrDefault(id => id.HasValue);
        var workflowTask = latestInstanceId.HasValue
            ? workflowLoader.LoadAsync(latestInstanceId.Value, includeHistory: false, cancellationToken)
            : Task.FromResult<(int, object?)>((404, null));
        var latestSigningId = sources.SigningRequests.Select(r => r.GetGuid("signingRequestId")).FirstOrDefault(id => id.HasValue);
        var signatureTask = latestSigningId.HasValue
            ? downstream.GetAsync(DownstreamServices.Signature, $"/api/SigningRequests/{latestSigningId}", cancellationToken)
            : Task.FromResult(new DownstreamResponse(404, null));
        await Task.WhenAll(timelineTask, workflowTask, signatureTask);

        return Ok(new
        {
            document = sources.Document,
            workflow = new { instances = sources.Instances, current = workflowTask.Result.Item2 },
            signature = new { requests = sources.SigningRequests, current = signatureTask.Result.IsSuccess ? signatureTask.Result.Body : null },
            permissions,
            timeline = timelineTask.Result,
            availableActions = DocumentWorkspaceComposer.ComputeAvailableActions(sources.Document, sources.Instances, sources.SigningRequests, permissions.ToHashSet()),
            unavailableSections = sources.Unavailable
        });
    }

    [HttpGet("timeline")]
    public async Task<IActionResult> Timeline(Guid documentId, CancellationToken cancellationToken)
    {
        var (status, sources) = await LoadSourcesAsync(documentId, cancellationToken);
        if (sources is null) return StatusCode(status);
        return Ok(new { items = await TimelineAsync(sources, cancellationToken), unavailableSections = sources.Unavailable });
    }

    [HttpGet("audit")]
    [Authorize(Policy = AuthorizationPolicies.GatewayAccess)]
    public async Task<IActionResult> Audit(Guid documentId, CancellationToken cancellationToken)
    {
        var organizationId = User.FindFirst("organization_id")?.Value;
        if (organizationId is null) return StatusCode(StatusCodes.Status403Forbidden);
        var (status, sources) = await LoadSourcesAsync(documentId, cancellationToken);
        if (sources is null) return StatusCode(status);

        var targets = new List<string> { documentId.ToString() };
        targets.AddRange(sources.Instances.Select(i => i.GetString("id")).OfType<string>());
        targets.AddRange(sources.SigningRequests.Select(r => r.GetString("signingRequestId")).OfType<string>());

        var responses = await Task.WhenAll(targets.Distinct().Select(target => downstream.GetAsync(DownstreamServices.Audit,
            $"/api/v1/AuditLogs?organizationId={organizationId}&entityId={target}&page=1&pageSize=100", cancellationToken)));
        if (responses.All(r => r.StatusCode == StatusCodes.Status403Forbidden)) return StatusCode(StatusCodes.Status403Forbidden);

        var items = responses.Where(r => r.IsSuccess).SelectMany(r => r.Body.Items())
            .OrderByDescending(log => log.GetDate("timestamp")).ToList();
        return Ok(new { items });
    }

    private async Task<(int Status, Sources? Sources)> LoadSourcesAsync(Guid documentId, CancellationToken cancellationToken)
    {
        var documentTask = downstream.GetAsync(DownstreamServices.Document, $"/api/v1/documents/{documentId}", cancellationToken);
        var instancesTask = downstream.GetAsync(DownstreamServices.Workflow, $"/api/v1/workflows/instances?documentId={documentId}&page=1&pageSize=10", cancellationToken);
        var signingTask = downstream.GetAsync(DownstreamServices.Signature, $"/api/SigningRequests?documentId={documentId}&page=1&pageSize=10", cancellationToken);
        await Task.WhenAll(documentTask, instancesTask, signingTask);

        if (!documentTask.Result.IsSuccess || documentTask.Result.Body is null) return (documentTask.Result.StatusCode, null);

        var unavailable = new List<string>();
        if (!instancesTask.Result.IsSuccess) unavailable.Add("workflow");
        if (!signingTask.Result.IsSuccess) unavailable.Add("signature");
        return (200, new Sources(
            documentTask.Result.Body.Value,
            instancesTask.Result.IsSuccess ? instancesTask.Result.Body.Items() : [],
            signingTask.Result.IsSuccess ? signingTask.Result.Body.Items() : [],
            unavailable));
    }

    private async Task<IReadOnlyList<TimelineEvent>> TimelineAsync(Sources sources, CancellationToken cancellationToken)
    {
        var histories = await Task.WhenAll(sources.Instances.Select(async instance =>
        {
            var id = instance.GetString("id");
            var response = await downstream.GetAsync(DownstreamServices.Workflow, $"/api/v1/workflows/instances/{id}/history?page=1&pageSize=100", cancellationToken);
            return (instance, history: response.IsSuccess ? response.Body.Items() : []);
        }));
        var details = await Task.WhenAll(sources.SigningRequests.Select(async request =>
        {
            var response = await downstream.GetAsync(DownstreamServices.Signature, $"/api/SigningRequests/{request.GetString("signingRequestId")}", cancellationToken);
            return response.IsSuccess ? response.Body : null;
        }));

        return DocumentWorkspaceComposer.BuildTimeline(
            sources.Document,
            histories.Select(h => (h.instance, h.history)).ToList(),
            details.Where(d => d.HasValue).Select(d => d!.Value).ToList());
    }

    private string[] Permissions() =>
        PermissionMapper.ToFrontend(User.FindAll("permission").Select(claim => claim.Value)).ToArray();
}
