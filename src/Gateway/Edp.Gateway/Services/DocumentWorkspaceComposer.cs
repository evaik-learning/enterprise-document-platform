using System.Text.Json;
using System.Text.RegularExpressions;

namespace Edp.Gateway.Services;

public sealed record TimelineEvent(
    string Id,
    DateTimeOffset Timestamp,
    string Type,
    string Title,
    string? Description,
    string Source,
    string? Actor,
    string Status,
    string? ResourceType,
    string? ResourceId);

/// <summary>Pure composition helpers for the document workspace; no business state is derived here.</summary>
public static partial class DocumentWorkspaceComposer
{
    private static readonly HashSet<string> ActiveWorkflowStatuses = new(StringComparer.OrdinalIgnoreCase) { "Pending", "InProgress", "AwaitingApproval", "Paused" };
    private static readonly HashSet<string> ActiveSigningStatuses = new(StringComparer.OrdinalIgnoreCase) { "Draft", "Pending", "InProgress", "PartiallySigned" };
    private static readonly HashSet<string> GenerateableStatuses = new(StringComparer.OrdinalIgnoreCase) { "Requested", "Draft", "Failed" };

    public static bool HasFiles(JsonElement document) =>
        document.Property("versions") is { ValueKind: JsonValueKind.Array } versions
        && versions.EnumerateArray().Any(version => version.Property("files") is { ValueKind: JsonValueKind.Array } files && files.GetArrayLength() > 0);

    public static IReadOnlyList<string> ComputeAvailableActions(
        JsonElement document,
        IReadOnlyList<JsonElement> workflowInstances,
        IReadOnlyList<JsonElement> signingRequests,
        IReadOnlySet<string> permissions)
    {
        var actions = new List<string>();
        var hasFiles = HasFiles(document);
        var status = document.GetString("status") ?? string.Empty;
        var activeWorkflow = workflowInstances.Any(instance => ActiveWorkflowStatuses.Contains(instance.GetString("status") ?? string.Empty));
        var activeSigning = signingRequests.Any(request => ActiveSigningStatuses.Contains(request.GetString("status") ?? string.Empty));
        var signed = signingRequests.Any(request => string.Equals(request.GetString("status"), "Completed", StringComparison.OrdinalIgnoreCase));

        if (hasFiles && permissions.Contains("document.read")) { actions.Add("PREVIEW"); actions.Add("DOWNLOAD"); }
        if (GenerateableStatuses.Contains(status) && permissions.Contains("document.generate")) actions.Add("GENERATE");
        if (hasFiles && !activeWorkflow && permissions.Contains("workflow.execute")) actions.Add("START_WORKFLOW");
        if (workflowInstances.Count > 0 && permissions.Contains("workflow.read")) actions.Add("VIEW_WORKFLOW");
        if (hasFiles && !activeSigning && !signed && permissions.Contains("signature.execute")) actions.Add("SEND_FOR_SIGNATURE");
        if (signingRequests.Count > 0 && permissions.Contains("signature.read")) actions.Add("VIEW_SIGNATURE");
        if (permissions.Contains("audit.read")) actions.Add("VIEW_AUDIT");
        return actions;
    }

    public static IReadOnlyList<TimelineEvent> BuildTimeline(
        JsonElement document,
        IReadOnlyList<(JsonElement Instance, IReadOnlyList<JsonElement> History)> workflows,
        IReadOnlyList<JsonElement> signingRequestDetails)
    {
        var events = new List<TimelineEvent>();
        var documentId = document.GetString("id");

        if (document.GetDate("createdAt") is { } created)
            events.Add(new($"doc-created-{documentId}", created, "DocumentCreated", "Document created", document.GetString("name"), "Document", null, "completed", "Document", documentId));

        if (document.Property("versions") is { ValueKind: JsonValueKind.Array } versions)
        {
            foreach (var version in versions.EnumerateArray())
            {
                if (version.GetDate("generatedAt") is not { } generated) continue;
                var number = version.GetInt("versionNumber");
                var failed = string.Equals(version.GetString("status"), "Failed", StringComparison.OrdinalIgnoreCase);
                events.Add(new($"doc-version-{version.GetString("id")}", generated, "DocumentGenerated",
                    failed ? $"Document generation failed (v{number})" : $"Document generated (v{number})",
                    $"Template version {version.GetInt("templateVersion")}", "Document", null, failed ? "failed" : "completed", "Document", documentId));
            }
        }

        foreach (var (instance, history) in workflows)
        {
            var instanceId = instance.GetString("id");
            foreach (var entry in history)
            {
                if (entry.GetDate("eventAt") is not { } at) continue;
                var type = entry.GetString("eventType") ?? "WorkflowEvent";
                events.Add(new($"wf-{entry.GetString("id")}", at, type, Humanize(type), entry.GetString("description"), "Workflow", entry.GetString("userId"),
                    type.Contains("Reject", StringComparison.OrdinalIgnoreCase) || type.Contains("Cancel", StringComparison.OrdinalIgnoreCase) || type.Contains("Fail", StringComparison.OrdinalIgnoreCase) ? "failed" : "completed",
                    "Workflow", instanceId));
            }
        }

        foreach (var request in signingRequestDetails)
        {
            var id = request.GetString("signingRequestId");
            if (request.GetDate("createdAt") is { } createdAt)
                events.Add(new($"sig-created-{id}", createdAt, "SignatureRequested", "Signature request created", request.GetString("title"), "Signature", null, "completed", "Signature", id));
            if (request.GetDate("activatedAt") is { } activated)
                events.Add(new($"sig-sent-{id}", activated, "SignatureSent", "Sent for signature", null, "Signature", null, "completed", "Signature", id));
            if (request.Property("auditTrail") is { ValueKind: JsonValueKind.Array } trail)
            {
                foreach (var entry in trail.EnumerateArray())
                {
                    if (entry.GetDate("occurredAt") is not { } at) continue;
                    var action = entry.GetString("actionType") ?? "Action";
                    events.Add(new($"sig-action-{entry.GetString("signatureActionId")}", at, $"Signature{action}", $"{Humanize(action)} by signer", entry.GetString("signerEmail"), "Signature", entry.GetString("signerEmail"),
                        action.Contains("Declin", StringComparison.OrdinalIgnoreCase) ? "failed" : "completed", "Signature", id));
                }
            }

            if (request.GetDate("completedAt") is { } completed)
                events.Add(new($"sig-completed-{id}", completed, "SignatureCompleted", "Signing completed", null, "Signature", null, "completed", "Signature", id));
        }

        return events.OrderBy(e => e.Timestamp).ToList();
    }

    public static string Humanize(string value) => PascalCaseBoundary().Replace(value, " $1").Trim();

    [GeneratedRegex("(?<!^)([A-Z])")]
    private static partial Regex PascalCaseBoundary();
}
