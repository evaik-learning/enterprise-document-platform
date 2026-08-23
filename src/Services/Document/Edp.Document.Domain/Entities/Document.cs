using Edp.Document.Domain.Enums;
using Edp.Document.Domain.Events;
using Edp.Document.Domain.Exceptions;
using Edp.SharedKernel.Entities;

namespace Edp.Document.Domain.Entities;

public sealed class Document : AuditableEntity<Guid>
{
    public Guid OrganizationId { get; private set; }
    public string DocumentType { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public Guid TemplateId { get; private set; }
    public int CurrentVersionNumber { get; private set; } = 1;
    public DocumentStatus Status { get; private set; } = DocumentStatus.Requested;
    public Guid? CurrentTemplateVersionId { get; private set; }
    public string? ExternalReference { get; private set; }
    public byte[]? RowVersion { get; private set; }

    private Document()
    {
    }

    public static Document Create(Guid organizationId, string documentType, string name, string templateIdString, Guid templateId, string? description)
    {
        if (organizationId == Guid.Empty)
        {
            throw new DocumentDomainException("OrganizationId is required.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(documentType);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var document = new Document
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            DocumentType = documentType.Trim(),
            Name = name.Trim(),
            Description = description?.Trim(),
            TemplateId = templateId,
            Status = DocumentStatus.Requested,
            CurrentVersionNumber = 1,
            CreatedBy = "system"
        };

        document.AddDomainEvent(new DocumentCreatedDomainEvent(document.Id, organizationId, templateId, document.Name));
        return document;
    }

    public void RequestGeneration()
    {
        if (Status is DocumentStatus.Deleted or DocumentStatus.Archived or DocumentStatus.Cancelled)
        {
            throw new DocumentDomainException($"Document cannot be requested for generation while in '{Status}' state.");
        }

        Status = DocumentStatus.Requested;
        ModifiedAt = DateTimeOffset.UtcNow;
    }

    public void MarkGenerating()
    {
        if (Status is not DocumentStatus.Requested and not DocumentStatus.Generated and not DocumentStatus.Draft and not DocumentStatus.InReview)
        {
            throw new DocumentDomainException($"Document cannot transition to '{DocumentStatus.Generating}' from '{Status}'.");
        }

        Status = DocumentStatus.Generating;
        ModifiedAt = DateTimeOffset.UtcNow;
    }

    public void MarkGenerated(int versionNumber, Guid templateVersionId)
    {
        if (Status != DocumentStatus.Generating)
        {
            throw new DocumentDomainException($"Document must be in '{DocumentStatus.Generating}' state before generation is completed.");
        }

        if (versionNumber < 1)
        {
            throw new DocumentDomainException("Version number must be at least 1.");
        }

        CurrentVersionNumber = versionNumber;
        CurrentTemplateVersionId = templateVersionId;
        Status = DocumentStatus.Generated;
        ModifiedAt = DateTimeOffset.UtcNow;
        AddDomainEvent(new DocumentGeneratedDomainEvent(Id, OrganizationId, versionNumber, templateVersionId));
    }

    public void FailGeneration(string errorMessage)
    {
        if (Status != DocumentStatus.Generating)
        {
            throw new DocumentDomainException($"Document must be in '{DocumentStatus.Generating}' state before failing generation.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage);

        Status = DocumentStatus.Failed;
        ModifiedAt = DateTimeOffset.UtcNow;
    }

    public void Archive()
    {
        if (Status != DocumentStatus.Generated && Status != DocumentStatus.Completed && Status != DocumentStatus.Archived)
        {
            throw new DocumentDomainException($"Document cannot be archived while in '{Status}' state. It must be generated or completed first.");
        }

        Status = DocumentStatus.Archived;
        ModifiedAt = DateTimeOffset.UtcNow;
        AddDomainEvent(new DocumentArchivedDomainEvent(Id, OrganizationId));
    }
}
