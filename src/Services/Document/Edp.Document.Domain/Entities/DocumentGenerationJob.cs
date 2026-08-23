using Edp.Document.Domain.Exceptions;
using Edp.SharedKernel.Entities;

namespace Edp.Document.Domain.Entities;

public sealed class DocumentGenerationJob : AuditableEntity<Guid>
{
    public Guid DocumentId { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid TemplateId { get; private set; }
    public string Status { get; private set; } = "Queued";
    public string? Error { get; private set; }
    public string FailureCategory { get; private set; } = "Unknown";
    public string CorrelationId { get; private set; } = string.Empty;
    public int Attempts { get; private set; }
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public byte[]? RowVersion { get; private set; }

    private DocumentGenerationJob()
    {
    }

    public static DocumentGenerationJob Create(Guid documentId, Guid organizationId, Guid templateId, string? correlationId = null)
    {
        if (documentId == Guid.Empty)
        {
            throw new DocumentDomainException("DocumentId is required.");
        }

        if (organizationId == Guid.Empty)
        {
            throw new DocumentDomainException("OrganizationId is required.");
        }

        if (templateId == Guid.Empty)
        {
            throw new DocumentDomainException("TemplateId is required.");
        }

        return new DocumentGenerationJob
        {
            Id = Guid.NewGuid(),
            DocumentId = documentId,
            OrganizationId = organizationId,
            TemplateId = templateId,
            Status = "Queued",
            CorrelationId = string.IsNullOrWhiteSpace(correlationId) ? Guid.NewGuid().ToString("N") : correlationId.Trim(),
            Attempts = 0,
            StartedAt = DateTimeOffset.UtcNow,
            FailureCategory = "Unknown"
        };
    }

    public void MarkProcessing()
    {
        if (Status == "Completed" || Status == "Failed")
        {
            return;
        }

        Status = "Processing";
        StartedAt ??= DateTimeOffset.UtcNow;
        ModifiedAt = DateTimeOffset.UtcNow;
    }

    public void MarkRetryScheduled(string error, string failureCategory = "Transient")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(error);
        if (Status == "Completed")
        {
            return;
        }

        Status = "Queued";
        Error = error;
        FailureCategory = NormalizeFailureCategory(failureCategory);
        Attempts++;
        CompletedAt = null;
        ModifiedAt = DateTimeOffset.UtcNow;
    }

    public void MarkCompleted()
    {
        Status = "Completed";
        Error = null;
        FailureCategory = "Unknown";
        CompletedAt = DateTimeOffset.UtcNow;
        ModifiedAt = DateTimeOffset.UtcNow;
    }

    public void MarkFailed(string error, string failureCategory = "Unknown")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(error);
        Status = "Failed";
        Error = error;
        FailureCategory = NormalizeFailureCategory(failureCategory);
        CompletedAt = DateTimeOffset.UtcNow;
        ModifiedAt = DateTimeOffset.UtcNow;
    }

    public bool CanRetry(int maxAttempts = 3)
    {
        return FailureCategory == "Transient" && Attempts < maxAttempts && Status != "Completed" && Status != "Failed";
    }

    private static string NormalizeFailureCategory(string? failureCategory)
    {
        if (string.IsNullOrWhiteSpace(failureCategory))
        {
            return "Unknown";
        }

        return failureCategory.Trim();
    }
}

