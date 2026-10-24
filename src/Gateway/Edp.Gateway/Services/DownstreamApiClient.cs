using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Edp.Gateway.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Edp.Gateway.Services;

/// <summary>Result of a downstream call; <see cref="Body"/> is null for empty or non-JSON bodies.</summary>
public sealed record DownstreamResponse(int StatusCode, JsonElement? Body)
{
    public bool IsSuccess => StatusCode is >= 200 and < 300;
}

/// <summary>Server-to-server client that forwards the caller's session context (token, tenant, correlation id).</summary>
public interface IDownstreamApiClient
{
    Task<DownstreamResponse> SendAsync(HttpMethod method, string service, string pathAndQuery, object? body = null, CancellationToken cancellationToken = default);
}

public static class DownstreamServices
{
    public const string Organization = "Organization";
    public const string Template = "Template";
    public const string Document = "Document";
    public const string Workflow = "Workflow";
    public const string Notification = "Notification";
    public const string Audit = "Audit";
    public const string Signature = "DigitalSignature";
}

public sealed class DownstreamApiClient(
    IHttpClientFactory clients,
    IOptions<GatewayOptions> options,
    IHttpContextAccessor accessor,
    ILogger<DownstreamApiClient> logger) : IDownstreamApiClient
{
    private static readonly JsonSerializerOptions BodyOptions = new(JsonSerializerDefaults.Web);

    public async Task<DownstreamResponse> SendAsync(HttpMethod method, string service, string pathAndQuery, object? body = null, CancellationToken cancellationToken = default)
    {
        var context = accessor.HttpContext ?? throw new InvalidOperationException("A request context is required for downstream calls.");
        var destination = options.Value.ServiceCommunication.DownstreamServices
            .FirstOrDefault(x => x.Key.Equals(service, StringComparison.OrdinalIgnoreCase)).Value;
        if (string.IsNullOrWhiteSpace(destination) || !Uri.TryCreate(destination, UriKind.Absolute, out _))
        {
            logger.LogWarning("Downstream service {Service} is not configured.", service);
            return new DownstreamResponse((int)HttpStatusCode.ServiceUnavailable, null);
        }

        using var request = new HttpRequestMessage(method, $"{destination.TrimEnd('/')}{pathAndQuery}");
        if (body is not null)
            request.Content = new StringContent(JsonSerializer.Serialize(body, BodyOptions), Encoding.UTF8, "application/json");

        var token = await context.GetTokenAsync("access_token");
        if (!string.IsNullOrWhiteSpace(token)) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var organizationId = context.User.FindFirst("organization_id")?.Value;
        if (!string.IsNullOrWhiteSpace(organizationId)) request.Headers.TryAddWithoutValidation("X-EDP-Organization", organizationId);
        var correlation = context.Items[options.Value.Correlation.HeaderName]?.ToString() ?? context.TraceIdentifier;
        request.Headers.TryAddWithoutValidation(options.Value.Correlation.HeaderName, correlation);

        try
        {
            using var response = await clients.CreateClient("DownstreamServices").SendAsync(request, cancellationToken);
            var text = await response.Content.ReadAsStringAsync(cancellationToken);
            JsonElement? json = null;
            if (!string.IsNullOrWhiteSpace(text))
            {
                try { json = JsonDocument.Parse(text).RootElement.Clone(); }
                catch (JsonException) { /* non-JSON body */ }
            }

            return new DownstreamResponse((int)response.StatusCode, json);
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(exception, "Downstream call to {Service} failed.", service);
            return new DownstreamResponse((int)HttpStatusCode.ServiceUnavailable, null);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new DownstreamResponse((int)HttpStatusCode.GatewayTimeout, null);
        }
    }
}

public static class DownstreamApiClientExtensions
{
    public static Task<DownstreamResponse> GetAsync(this IDownstreamApiClient client, string service, string pathAndQuery, CancellationToken cancellationToken = default) =>
        client.SendAsync(HttpMethod.Get, service, pathAndQuery, null, cancellationToken);
}
