using Edp.SharedKernel.Entities;
using Edp.Template.Domain.Enums;
using Edp.Template.Domain.Events;
using Edp.Template.Domain.Exceptions;

namespace Edp.Template.Domain.Entities;

/// <summary>An immutable, independently stored binary revision of a <see cref="Template"/>.</summary>
public sealed class TemplateVersion : AuditableEntity<Guid>
{
    public Guid TemplateId { get; private set; }
    public Guid OrganizationId { get; private set; }
    public int VersionNumber { get; private set; }
    public string FileName { get; private set; } = string.Empty;
    public string StoragePath { get; private set; } = string.Empty;
    public string BlobContainer { get; private set; } = string.Empty;
    public string? FileHash { get; private set; }
    public long FileSize { get; private set; }
    public string ContentType { get; private set; } = string.Empty;
    public ValidationStatus ValidationStatus { get; private set; } = ValidationStatus.NotValidated;
    public TemplateVersionStatus Status { get; private set; } = TemplateVersionStatus.Uploaded;
    public string? ChangeDescription { get; private set; }
    public byte[]? RowVersion { get; private set; }

    private TemplateVersion()
    {
    }

    public static TemplateVersion Create(
        Guid id,
        Guid templateId,
        Guid organizationId,
        int versionNumber,
        string fileName,
        string blobContainer,
        string storagePath,
        string fileHash,
        long fileSize,
        string contentType,
        string? changeDescription,
        Guid? createdBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(storagePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);

        if (versionNumber < 1)
        {
            throw new TemplateDomainException("Version numbers must start at 1 and increase sequentially.");
        }

        var version = new TemplateVersion
        {
            Id = id,
            TemplateId = templateId,
            OrganizationId = organizationId,
            VersionNumber = versionNumber,
            FileName = fileName,
            BlobContainer = blobContainer,
            StoragePath = storagePath,
            FileHash = fileHash,
            FileSize = fileSize,
            ContentType = contentType,
            ChangeDescription = changeDescription,
            ValidationStatus = ValidationStatus.NotValidated,
            Status = TemplateVersionStatus.Uploaded,
            CreatedBy = createdBy?.ToString()
        };

        version.AddDomainEvent(new TemplateVersionCreatedDomainEvent(templateId, version.Id, organizationId, versionNumber, createdBy));
        return version;
    }

    public void MarkValidated(bool isValid)
    {
        ValidationStatus = isValid ? ValidationStatus.Valid : ValidationStatus.Invalid;
        ModifiedAt = DateTimeOffset.UtcNow;

        AddDomainEvent(new TemplateValidatedDomainEvent(TemplateId, Id, OrganizationId, isValid));
    }

    public void Activate()
    {
        if (ValidationStatus != ValidationStatus.Valid)
        {
            throw new TemplateDomainException("Only a version that has passed validation may be activated.");
        }

        Status = TemplateVersionStatus.Active;
        ModifiedAt = DateTimeOffset.UtcNow;
    }

    public void Deactivate()
    {
        if (Status == TemplateVersionStatus.Active)
        {
            Status = TemplateVersionStatus.Inactive;
            ModifiedAt = DateTimeOffset.UtcNow;
        }
    }
}
