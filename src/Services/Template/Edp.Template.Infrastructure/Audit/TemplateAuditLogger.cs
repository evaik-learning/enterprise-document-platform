using System.Net;
using System.Text;
using System.Text.Json;
using Edp.Template.Application.Contracts;
using Microsoft.Extensions.Configuration;

namespace Edp.Template.Infrastructure.Audit;

public sealed class TemplateAuditLogger : ITemplateAuditLogger
{
    private readonly HttpClient _httpClient;
    private readonly string _auditEndpoint;

    public TemplateAuditLogger(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _auditEndpoint = configuration["Audit:Endpoint"] ?? "http://localhost:7008/api/v1/AuditLogs";
    }

    public async Task RecordAsync(
        Guid organizationId,
        Guid? userId,
        string action,
        string entityType,
        Guid entityId,
        Dictionary<string, object?>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(action))
        {
            throw new ArgumentException("Audit action is required.", nameof(action));
        }

        if (string.IsNullOrWhiteSpace(entityType))
        {
            throw new ArgumentException("Audit entity type is required.", nameof(entityType));
        }

        var request = new
        {
            organizationId,
            userId,
            action,
            entityType,
            entityId,
            correlationId = Guid.NewGuid().ToString("N"),
            ipAddress = IPAddress.Loopback.ToString(),
            metadata
        };

        var content = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json");

        using var response = await _httpClient.PostAsync(_auditEndpoint, content, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Template audit logging failed: {(int)response.StatusCode} {response.ReasonPhrase}. Details: {error}");
        }
    }
}
