using System.Text.Json;

namespace Edp.Gateway.Services;

/// <summary>Loads a workflow instance with its definition graph, tasks and history in one composed view.</summary>
public sealed class WorkflowContextLoader(IDownstreamApiClient downstream)
{
    public async Task<(int StatusCode, object? Context)> LoadAsync(Guid instanceId, bool includeHistory, CancellationToken cancellationToken)
    {
        var instanceResponse = await downstream.GetAsync(DownstreamServices.Workflow, $"/api/v1/workflows/instances/{instanceId}", cancellationToken);
        if (!instanceResponse.IsSuccess) return (instanceResponse.StatusCode, null);
        var instance = instanceResponse.Body!.Value;

        var workflowId = instance.GetGuid("workflowId");
        var version = instance.GetInt("workflowVersion");
        var currentStateId = instance.GetGuid("currentStateId");

        var tasksTask = downstream.GetAsync(DownstreamServices.Workflow, $"/api/v1/workflows/instances/{instanceId}/approval-tasks", cancellationToken);
        var historyTask = includeHistory
            ? downstream.GetAsync(DownstreamServices.Workflow, $"/api/v1/workflows/instances/{instanceId}/history?page=1&pageSize=100", cancellationToken)
            : Task.FromResult(new DownstreamResponse(204, null));
        var workflowTask = workflowId.HasValue
            ? downstream.GetAsync(DownstreamServices.Workflow, $"/api/v1/workflows/{workflowId}", cancellationToken)
            : Task.FromResult(new DownstreamResponse(404, null));
        var versionsTask = workflowId.HasValue
            ? downstream.GetAsync(DownstreamServices.Workflow, $"/api/v1/workflows/{workflowId}/versions?page=1&pageSize=50", cancellationToken)
            : Task.FromResult(new DownstreamResponse(404, null));
        await Task.WhenAll(tasksTask, historyTask, workflowTask, versionsTask);

        var versionId = versionsTask.Result.IsSuccess
            ? versionsTask.Result.Body.Items().FirstOrDefault(v => v.GetInt("version") == version).GetGuid("id")
            : null;
        IReadOnlyList<JsonElement> states = [];
        IReadOnlyList<JsonElement> transitions = [];
        if (versionId.HasValue)
        {
            var graph = await downstream.GetAsync(DownstreamServices.Workflow, $"/api/v1/workflows/versions/{versionId}/graph", cancellationToken);
            if (graph.IsSuccess)
            {
                states = graph.Body.Property("states") is { ValueKind: JsonValueKind.Array } s ? s.EnumerateArray().ToList() : [];
                transitions = graph.Body.Property("transitions") is { ValueKind: JsonValueKind.Array } t ? t.EnumerateArray().ToList() : [];
            }
        }

        var currentState = currentStateId.HasValue ? states.Where(state => state.GetGuid("id") == currentStateId).Select(state => (JsonElement?)state).FirstOrDefault() : null;
        var nextTransitions = currentStateId.HasValue ? transitions.Where(transition => transition.GetGuid("fromStateId") == currentStateId).ToList() : [];

        return (200, new
        {
            instance,
            workflow = workflowTask.Result.IsSuccess ? workflowTask.Result.Body : null,
            states,
            transitions,
            currentState,
            nextTransitions,
            tasks = tasksTask.Result.IsSuccess ? tasksTask.Result.Body.Items() : [],
            history = historyTask.Result.IsSuccess ? historyTask.Result.Body.Items() : []
        });
    }
}
