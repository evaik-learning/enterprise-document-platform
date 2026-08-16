using Edp.SharedKernel.Entities;
using Edp.Template.Domain.Enums;
using Edp.Template.Domain.Events;
using Edp.Template.Domain.Exceptions;

namespace Edp.Template.Domain.Entities;

/// <summary>Aggregate root for a logical document template scoped to a single organization.</summary>
public sealed class Template : AuditableEntity<Guid>
{
    public Guid OrganizationId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Code { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public TemplateStatus Status { get; private set; } = TemplateStatus.Draft;
    public Guid? CurrentVersionId { get; private set; }
    public byte[]? RowVersion { get; private set; }

    private Template()
    {
    }

    public static Template Create(Guid id, Guid organizationId, string name, string code, string? description, Guid? createdBy)
    {
        if (organizationId == Guid.Empty)
        {
            throw new TemplateDomainException("OrganizationId is required.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        var template = new Template
        {
            Id = id,
            OrganizationId = organizationId,
            Name = name.Trim(),
            Code = NormalizeCode(code),
            Description = description?.Trim(),
            Status = TemplateStatus.Draft,
            CreatedBy = createdBy?.ToString()
        };

        template.AddDomainEvent(new TemplateCreatedDomainEvent(template.Id, organizationId, template.Code, createdBy));
        return template;
    }

    public static string NormalizeCode(string code) => code.Trim().ToUpperInvariant();

    public void UpdateDetails(string name, string? description, Guid? updatedBy)
    {
        EnsureMutable();
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Name = name.Trim();
        Description = description?.Trim();
        ModifiedAt = DateTimeOffset.UtcNow;
        ModifiedBy = updatedBy?.ToString();

        AddDomainEvent(new TemplateUpdatedDomainEvent(Id, OrganizationId, updatedBy));
    }

    public void ActivateVersion(Guid versionId, Guid? activatedBy)
    {
        if (Status == TemplateStatus.Archived)
        {
            throw new TemplateDomainException("An archived template cannot be activated.");
        }

        CurrentVersionId = versionId;
        Status = TemplateStatus.Active;
        ModifiedAt = DateTimeOffset.UtcNow;
        ModifiedBy = activatedBy?.ToString();

        AddDomainEvent(new TemplateActivatedDomainEvent(Id, versionId, OrganizationId, activatedBy));
    }

    public void Deactivate(Guid? deactivatedBy)
    {
        if (Status == TemplateStatus.Archived)
        {
            throw new TemplateDomainException("An archived template cannot be deactivated.");
        }

        Status = TemplateStatus.Inactive;
        ModifiedAt = DateTimeOffset.UtcNow;
        ModifiedBy = deactivatedBy?.ToString();

        AddDomainEvent(new TemplateDeactivatedDomainEvent(Id, OrganizationId, deactivatedBy));
    }

    public void Archive(Guid? archivedBy)
    {
        if (Status == TemplateStatus.Archived)
        {
            return;
        }

        Status = TemplateStatus.Archived;
        ModifiedAt = DateTimeOffset.UtcNow;
        ModifiedBy = archivedBy?.ToString();

        AddDomainEvent(new TemplateArchivedDomainEvent(Id, OrganizationId, archivedBy));
    }

    private void EnsureMutable()
    {
        if (Status == TemplateStatus.Archived)
        {
            throw new TemplateDomainException("An archived template cannot be modified.");
        }
    }
}
