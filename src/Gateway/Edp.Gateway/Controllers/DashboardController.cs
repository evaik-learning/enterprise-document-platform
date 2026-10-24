using System.Text.Json;
using Edp.Gateway.Security;
using Edp.Gateway.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Edp.Gateway.Controllers;

/// <summary>Aggregates dashboard widgets from several services; sections that fail are reported, not fatal.</summary>
[ApiController]
[Authorize(Policy = AuthorizationPolicies.GatewayAccess)]
[Route("api/v1/dashboard")]
public sealed class DashboardController(IDownstreamApiClient downstream) : ControllerBase
{
    private static readonly HashSet<string> OpenSigningStatuses = new(StringComparer.OrdinalIgnoreCase) { "Pending", "InProgress", "PartiallySigned" };

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var organizationId = User.FindFirst("organization_id")?.Value;
        if (organizationId is null)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { code = "ORGANIZATION_REQUIRED", message = "Select an organization to view the dashboard.", correlationId = HttpContext.TraceIdentifier, details = Array.Empty<object>() });
        }

        var templates = downstream.GetAsync(DownstreamServices.Template, "/api/v1/templates?status=Active&page=1&pageSize=1", cancellationToken);
        var documents = downstream.GetAsync(DownstreamServices.Document, "/api/v1/documents?page=1&pageSize=5", cancellationToken);
        var completed = downstream.GetAsync(DownstreamServices.Document, "/api/v1/documents?status=Completed&page=1&pageSize=1", cancellationToken);
        var approvals = downstream.GetAsync(DownstreamServices.Workflow, "/api/v1/workflows/approval-tasks/my?page=1&pageSize=50", cancellationToken);
        var signatures = downstream.GetAsync(DownstreamServices.Signature, "/api/SigningRequests?page=1&pageSize=50", cancellationToken);
        var unread = downstream.GetAsync(DownstreamServices.Notification, "/api/v1/notifications/unread-count", cancellationToken);
        var notifications = downstream.GetAsync(DownstreamServices.Notification, "/api/v1/notifications?page=1&pageSize=5", cancellationToken);
        var activity = downstream.GetAsync(DownstreamServices.Audit, $"/api/v1/AuditLogs?organizationId={organizationId}&page=1&pageSize=5", cancellationToken);
        await Task.WhenAll(templates, documents, completed, approvals, signatures, unread, notifications, activity);

        var unavailable = new List<string>();
        T? Section<T>(string name, DownstreamResponse response, Func<DownstreamResponse, T> project)
        {
            if (response.IsSuccess) return project(response);
            unavailable.Add(name);
            return default;
        }

        var pendingApprovals = Section("approvals", approvals.Result, r => r.Body.Items()
            .Where(task => string.Equals(task.GetString("status"), "Pending", StringComparison.OrdinalIgnoreCase)).ToList());
        var openSignatures = Section("signatures", signatures.Result, r => r.Body.Items()
            .Where(request => OpenSigningStatuses.Contains(request.GetString("status") ?? string.Empty)).ToList());

        var counts = new
        {
            templates = Section("templates", templates.Result, r => (int?)r.Body.TotalCount()),
            documents = Section("documents", documents.Result, r => (int?)r.Body.TotalCount()),
            completedDocuments = Section("completedDocuments", completed.Result, r => (int?)r.Body.TotalCount()),
            pendingApprovals = pendingApprovals?.Count,
            awaitingSignature = openSignatures?.Count,
            unreadNotifications = Section("notifications", unread.Result, r => (int?)(r.Body.GetInt("count") ?? 0))
        };

        return Ok(new
        {
            counts,
            recentDocuments = Section("recentDocuments", documents.Result, r => r.Body.Items()),
            pendingApprovals = pendingApprovals?.Take(5),
            pendingSignatures = openSignatures?.Take(5),
            recentActivity = Section("activity", activity.Result, r => r.Body.Items()),
            recentNotifications = Section("recentNotifications", notifications.Result, r => r.Body.Items()),
            unavailableSections = unavailable.Distinct().ToArray()
        });
    }
}
