using System.Security.Claims;
using System.Text.Json;
using Edp.Gateway.Controllers;
using Edp.Gateway.Security;
using Edp.Gateway.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Edp.Gateway.Tests;

public sealed class Phase10GatewayTests
{
    private static readonly Guid OrganizationId = Guid.NewGuid();

    [Fact]
    public void PermissionMapperTranslatesBackendCatalogToFrontendNames()
    {
        var mapped = PermissionMapper.ToFrontend(["Templates.Read", "Documents.Write", "Approval.Approve", "Members.Manage", "Organization.Manage", "Unknown.Thing"]);

        Assert.Equal(["document.generate", "organization.admin", "template.read", "workflow.approve"], mapped);
    }

    [Fact]
    public void AvailableActionsFollowDocumentStateAndPermissions()
    {
        var document = Json("""{"status":"Generated","versions":[{"files":[{"id":"f"}]}]}""");
        var all = new HashSet<string> { "document.read", "document.generate", "workflow.execute", "workflow.read", "signature.execute", "signature.read", "audit.read" };

        var actions = DocumentWorkspaceComposer.ComputeAvailableActions(document, [], [], all);

        Assert.Contains("DOWNLOAD", actions);
        Assert.Contains("START_WORKFLOW", actions);
        Assert.Contains("SEND_FOR_SIGNATURE", actions);
        Assert.Contains("VIEW_AUDIT", actions);
        Assert.DoesNotContain("GENERATE", actions);
        Assert.DoesNotContain("VIEW_WORKFLOW", actions);
    }

    [Fact]
    public void ActiveWorkflowAndSigningRequestSuppressStartActions()
    {
        var document = Json("""{"status":"PendingApproval","versions":[{"files":[{"id":"f"}]}]}""");
        var instances = new[] { Json("""{"id":"i1","status":"AwaitingApproval"}""") };
        var requests = new[] { Json("""{"signingRequestId":"s1","status":"InProgress"}""") };
        var all = new HashSet<string> { "document.read", "workflow.execute", "workflow.read", "signature.execute", "signature.read" };

        var actions = DocumentWorkspaceComposer.ComputeAvailableActions(document, instances, requests, all);

        Assert.DoesNotContain("START_WORKFLOW", actions);
        Assert.DoesNotContain("SEND_FOR_SIGNATURE", actions);
        Assert.Contains("VIEW_WORKFLOW", actions);
        Assert.Contains("VIEW_SIGNATURE", actions);
    }

    [Fact]
    public void NoPermissionsMeansNoActions()
    {
        var document = Json("""{"status":"Requested","versions":[]}""");

        Assert.Empty(DocumentWorkspaceComposer.ComputeAvailableActions(document, [], [], new HashSet<string>()));
    }

    [Fact]
    public void TimelineMergesDocumentWorkflowAndSignatureEventsChronologically()
    {
        var document = Json("""{"id":"d1","name":"Contract","createdAt":"2026-01-01T10:00:00Z","versions":[{"id":"v1","versionNumber":1,"templateVersion":2,"status":"Completed","generatedAt":"2026-01-01T10:05:00Z"}]}""");
        var instance = Json("""{"id":"w1"}""");
        var history = new[] { Json("""{"id":"h1","eventType":"ApprovalApproved","description":"Approved","eventAt":"2026-01-01T10:10:00Z","userId":"u1"}""") };
        var signing = Json("""{"signingRequestId":"s1","title":"Sign","createdAt":"2026-01-01T10:20:00Z","completedAt":"2026-01-01T10:40:00Z","auditTrail":[{"signatureActionId":"a1","actionType":"Signed","occurredAt":"2026-01-01T10:30:00Z","signerEmail":"a@b.c"}]}""");

        var timeline = DocumentWorkspaceComposer.BuildTimeline(document, [(instance, history)], [signing]);

        Assert.Equal(
            ["DocumentCreated", "DocumentGenerated", "ApprovalApproved", "SignatureRequested", "SignatureSigned", "SignatureCompleted"],
            timeline.Select(e => e.Type));
        Assert.Equal("Approval Approved", timeline[2].Title);
        Assert.Equal(["Document", "Document", "Workflow", "Signature", "Signature", "Signature"], timeline.Select(e => e.Source));
    }

    [Fact]
    public async Task DashboardAggregatesCountsAndReportsUnavailableSections()
    {
        var downstream = new FakeDownstream()
            .On("/api/v1/templates", 200, """{"items":[],"totalCount":42}""")
            .On("/api/v1/documents?page=1&pageSize=5", 200, """{"items":[{"id":"d1"}],"totalCount":1284}""")
            .On("/api/v1/documents?status=Completed", 200, """{"items":[],"totalCount":964}""")
            .On("/api/v1/workflows/approval-tasks/my", 200, """[{"id":"t1","status":"Pending"},{"id":"t2","status":"Approved"}]""")
            .On("/api/SigningRequests", 200, """{"items":[{"status":"InProgress"},{"status":"Completed"}],"totalCount":2}""")
            .On("/api/v1/notifications/unread-count", 200, """{"count":5}""")
            .On("/api/v1/notifications?page=1", 200, """{"items":[{"id":"n1"}]}""");
        // Audit is intentionally not configured -> 503.
        var controller = Controller(new DashboardController(downstream), "template.read");

        var result = Assert.IsType<OkObjectResult>(await controller.Get(CancellationToken.None));
        var json = JsonSerializer.SerializeToElement(result.Value);

        Assert.Equal(42, json.GetProperty("counts").GetProperty("templates").GetInt32());
        Assert.Equal(1284, json.GetProperty("counts").GetProperty("documents").GetInt32());
        Assert.Equal(964, json.GetProperty("counts").GetProperty("completedDocuments").GetInt32());
        Assert.Equal(1, json.GetProperty("counts").GetProperty("pendingApprovals").GetInt32());
        Assert.Equal(1, json.GetProperty("counts").GetProperty("awaitingSignature").GetInt32());
        Assert.Equal(5, json.GetProperty("counts").GetProperty("unreadNotifications").GetInt32());
        Assert.Contains("activity", json.GetProperty("unavailableSections").EnumerateArray().Select(x => x.GetString()));
    }

    [Fact]
    public async Task DashboardRequiresOrganizationContext()
    {
        var controller = new DashboardController(new FakeDownstream()) { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity("test")) } } };

        var result = await controller.Get(CancellationToken.None);

        Assert.Equal(StatusCodes.Status403Forbidden, Assert.IsType<ObjectResult>(result).StatusCode);
    }

    [Fact]
    public async Task SearchOnlyQueriesGroupsThePrincipalMayRead()
    {
        var downstream = new FakeDownstream()
            .On("/api/v1/templates?search=inv", 200, """{"items":[{"id":"t1","name":"Invoice","code":"INV","status":"Active"}],"totalCount":1}""")
            .On("/api/v1/documents?search=inv", 200, """{"items":[],"totalCount":0}""");
        var controller = Controller(new SearchController(downstream), "template.read");

        var result = Assert.IsType<OkObjectResult>(await controller.Search("inv", null, CancellationToken.None));
        var json = JsonSerializer.SerializeToElement(result.Value, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        var groups = json.GetProperty("groups").EnumerateArray().ToList();
        Assert.Single(groups);
        Assert.Equal("templates", groups[0].GetProperty("type").GetString());
        Assert.Equal("/templates/t1", groups[0].GetProperty("items")[0].GetProperty("url").GetString());
        Assert.DoesNotContain(downstream.Requests, path => path.StartsWith("/api/v1/documents"));
    }

    [Fact]
    public async Task SearchRejectsShortQueries()
    {
        var controller = Controller(new SearchController(new FakeDownstream()), "template.read");

        Assert.IsType<BadRequestObjectResult>(await controller.Search("a", null, CancellationToken.None));
    }

    [Fact]
    public async Task MeReturnsFrontendPermissionsAndTenant()
    {
        var controller = new MeController(new FakeDownstream().On($"/api/v1/Organizations/{OrganizationId}", 200, """{"id":"x","name":"Acme"}"""))
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                    [
                        new Claim("oid", "11111111-1111-1111-1111-111111111111"),
                        new Claim("name", "John Doe"),
                        new Claim("organization_id", OrganizationId.ToString()),
                        new Claim("edp_role", "Member"),
                        new Claim("permission", "Templates.Read"),
                        new Claim("permission", "Documents.Write")
                    ], "test"))
                }
            }
        };

        var response = Assert.IsType<OkObjectResult>((await controller.Get(CancellationToken.None)).Result);
        var me = Assert.IsType<MeResponse>(response.Value);

        Assert.Equal("Acme", me.OrganizationName);
        Assert.False(me.RequiresOrganizationSelection);
        Assert.Equal(["document.generate", "template.read"], me.Permissions);
        Assert.Equal(["Member"], me.Roles);
    }

    private static T Controller<T>(T controller, params string[] backendPermissions) where T : ControllerBase
    {
        var claims = new List<Claim> { new("organization_id", OrganizationId.ToString()) };
        claims.AddRange(backendPermissions.Select(p => new Claim("permission", p switch
        {
            "template.read" => "Templates.Read",
            "document.read" => "Documents.Read",
            _ => p
        })));
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test")) } };
        return controller;
    }

    private static JsonElement Json(string json) => JsonDocument.Parse(json).RootElement.Clone();

    private sealed class FakeDownstream : IDownstreamApiClient
    {
        private readonly List<(string Prefix, DownstreamResponse Response)> _routes = [];
        public List<string> Requests { get; } = [];

        public FakeDownstream On(string prefix, int status, string body)
        {
            _routes.Add((prefix, new DownstreamResponse(status, Json(body))));
            return this;
        }

        public Task<DownstreamResponse> SendAsync(HttpMethod method, string service, string pathAndQuery, object? body = null, CancellationToken cancellationToken = default)
        {
            Requests.Add(pathAndQuery);
            var match = _routes.FirstOrDefault(route => pathAndQuery.StartsWith(route.Prefix, StringComparison.OrdinalIgnoreCase));
            return Task.FromResult(match.Response ?? new DownstreamResponse(503, null));
        }
    }
}
