using System.Text.Json;
using Edp.Gateway.Security;
using Edp.Gateway.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Edp.Gateway.Controllers;

/// <summary>Global search across tenant-scoped resources the caller is permitted to read.</summary>
[ApiController]
[Authorize(Policy = AuthorizationPolicies.GatewayAccess)]
[Route("api/v1/search")]
public sealed class SearchController(IDownstreamApiClient downstream) : ControllerBase
{
    private const int MaxResultsPerGroup = 5;

    private sealed record SearchItem(string Id, string Title, string? Subtitle, string? Status, string Url);
    private sealed record SearchGroup(string Type, int TotalCount, IReadOnlyList<SearchItem> Items);

    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] string? q, [FromQuery] string? type, CancellationToken cancellationToken)
    {
        var term = q?.Trim();
        if (string.IsNullOrWhiteSpace(term) || term.Length < 2)
            return BadRequest(new { code = "SEARCH_QUERY_TOO_SHORT", message = "Enter at least 2 characters to search.", correlationId = HttpContext.TraceIdentifier, details = Array.Empty<object>() });
        if (term.Length > 100) term = term[..100];
        if (User.FindFirst("organization_id") is null)
            return StatusCode(StatusCodes.Status403Forbidden, new { code = "ORGANIZATION_REQUIRED", message = "Select an organization to search.", correlationId = HttpContext.TraceIdentifier, details = Array.Empty<object>() });

        var permissions = PermissionMapper.ToFrontend(User.FindAll("permission").Select(claim => claim.Value)).ToHashSet();
        bool Wants(string group, string permission) =>
            permissions.Contains(permission) && (string.IsNullOrWhiteSpace(type) || type.Equals(group, StringComparison.OrdinalIgnoreCase));

        var encoded = Uri.EscapeDataString(term);
        var tasks = new List<Task<SearchGroup?>>();

        if (Wants("templates", "template.read"))
            tasks.Add(Group("templates", DownstreamServices.Template, $"/api/v1/templates?search={encoded}&page=1&pageSize={MaxResultsPerGroup}",
                items => items, item => new SearchItem(item.GetString("id") ?? "", item.GetString("name") ?? "Template", item.GetString("code"), item.GetString("status"), $"/templates/{item.GetString("id")}"), cancellationToken));
        if (Wants("documents", "document.read"))
            tasks.Add(Group("documents", DownstreamServices.Document, $"/api/v1/documents?search={encoded}&page=1&pageSize={MaxResultsPerGroup}",
                items => items, item => new SearchItem(item.GetString("id") ?? "", item.GetString("name") ?? "Document", item.GetString("documentType"), item.GetString("status"), $"/documents/{item.GetString("id")}"), cancellationToken));
        if (Wants("workflows", "workflow.read"))
            tasks.Add(Group("workflows", DownstreamServices.Workflow, "/api/v1/workflows?page=1&pageSize=100",
                items => items.Where(item => Contains(item.GetString("name"), term) || Contains(item.GetString("code"), term)).ToList(),
                item => new SearchItem(item.GetString("id") ?? "", item.GetString("name") ?? "Workflow", item.GetString("code"), item.GetString("status"), "/workflows"), cancellationToken));
        if (Wants("signatures", "signature.read"))
            tasks.Add(Group("signatures", DownstreamServices.Signature, "/api/SigningRequests?page=1&pageSize=50",
                items => items.Where(item => Contains(item.GetString("title"), term)).ToList(),
                item => new SearchItem(item.GetString("signingRequestId") ?? "", item.GetString("title") ?? "Signature request", null, item.GetString("status"), $"/signing/{item.GetString("signingRequestId")}"), cancellationToken));

        var groups = (await Task.WhenAll(tasks)).Where(group => group is not null).Cast<SearchGroup>().ToList();
        return Ok(new { query = term, groups });
    }

    private async Task<SearchGroup?> Group(
        string name,
        string service,
        string path,
        Func<IReadOnlyList<JsonElement>, IReadOnlyList<JsonElement>> filter,
        Func<JsonElement, SearchItem> map,
        CancellationToken cancellationToken)
    {
        var response = await downstream.GetAsync(service, path, cancellationToken);
        if (!response.IsSuccess) return null;
        var filtered = filter(response.Body.Items());
        var total = response.Body.GetInt("totalCount") is { } serverTotal && filtered.Count == response.Body.Items().Count ? serverTotal : filtered.Count;
        return new SearchGroup(name, total, filtered.Take(MaxResultsPerGroup).Select(map).ToList());
    }

    private static bool Contains(string? value, string term) =>
        value?.Contains(term, StringComparison.OrdinalIgnoreCase) == true;
}
