using System.Net;
using System.Net.Http.Headers;
using Edp.Gateway.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Edp.Gateway.Controllers;

/// <summary>
/// Browser-facing, allow-listed BFF proxy. Downstream URLs and credentials never reach the browser.
/// </summary>
[ApiController]
[Authorize(Policy = Security.AuthorizationPolicies.GatewayAccess)]
[Route("api/v1/{service}/{**path}")]
public sealed class ServiceProxyController : ControllerBase
{
    private static readonly HashSet<string> AllowedServices = new(StringComparer.OrdinalIgnoreCase)
    {
        "templates", "documents", "workflows", "notifications", "audit-logs", "organizations", "signing-requests"
    };

    private readonly IHttpClientFactory _clients;
    private readonly GatewayOptions _options;

    public ServiceProxyController(IHttpClientFactory clients, IOptions<GatewayOptions> options)
    {
        _clients = clients;
        _options = options.Value;
    }

    [AcceptVerbs("GET", "POST", "PUT", "PATCH", "DELETE", "HEAD")]
    public async Task Proxy(string service, string? path, CancellationToken cancellationToken)
    {
        if (!AllowedServices.Contains(service) || !TryResolveDestination(service, out var destination))
        {
            Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        var suffix = string.IsNullOrWhiteSpace(path) ? string.Empty : $"/{path}";
        var downstreamResource = service switch
        {
            "audit-logs" => "AuditLogs",
            "organizations" => "Organizations",
            _ => service
        };
        var servicePath = service.Equals("signing-requests", StringComparison.OrdinalIgnoreCase)
            ? $"/api/SigningRequests{suffix}"
            : $"/api/v1/{downstreamResource}{suffix}";
        var target = new Uri($"{destination.TrimEnd('/')}{servicePath}{Request.QueryString}");
        using var message = new HttpRequestMessage(new HttpMethod(Request.Method), target);
        if (Request.ContentLength is > 0 || Request.Headers.ContainsKey("Transfer-Encoding"))
            message.Content = new StreamContent(Request.Body);

        foreach (var header in Request.Headers)
        {
            if (header.Key.Equals("Host", StringComparison.OrdinalIgnoreCase)
                || header.Key.Equals("Cookie", StringComparison.OrdinalIgnoreCase)
                || header.Key.Equals("Content-Length", StringComparison.OrdinalIgnoreCase)) continue;
            if (!message.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray()))
                message.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
        }

        // Never trust a browser-supplied tenant header; derive it from the authenticated Gateway session.
        message.Headers.Remove("X-EDP-Organization");
        var organizationId = User.FindFirst("organization_id")?.Value;
        if (!string.IsNullOrWhiteSpace(organizationId))
            message.Headers.TryAddWithoutValidation("X-EDP-Organization", organizationId);

        var accessToken = await HttpContext.GetTokenAsync("access_token");
        if (!string.IsNullOrWhiteSpace(accessToken))
            message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var correlation = Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? HttpContext.TraceIdentifier;
        message.Headers.TryAddWithoutValidation("X-Correlation-ID", correlation);

        var client = _clients.CreateClient("DownstreamServices");
        using var response = await client.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        Response.StatusCode = (int)response.StatusCode;
        foreach (var header in response.Headers)
            Response.Headers[header.Key] = header.Value.ToArray();
        foreach (var header in response.Content.Headers)
            Response.Headers[header.Key] = header.Value.ToArray();
        Response.Headers.Remove("transfer-encoding");
        await response.Content.CopyToAsync(Response.Body, cancellationToken);
    }

    private bool TryResolveDestination(string service, out string destination)
    {
        var key = service switch
        {
            "audit-logs" => "Audit",
            "signing-requests" => "DigitalSignature",
            _ => char.ToUpperInvariant(service[0]) + service[1..]
        };
        var configured = _options.ServiceCommunication.DownstreamServices;
        var match = configured.FirstOrDefault(x => x.Key.Equals(key, StringComparison.OrdinalIgnoreCase)).Value;
        destination = match ?? string.Empty;
        return !string.IsNullOrWhiteSpace(destination) && Uri.TryCreate(destination, UriKind.Absolute, out _);
    }
}
