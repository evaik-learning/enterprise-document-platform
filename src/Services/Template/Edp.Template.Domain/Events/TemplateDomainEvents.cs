using Edp.SharedKernel.Domain;

namespace Edp.Template.Domain.Events;

public sealed record TemplateCreatedDomainEvent(Guid TemplateId, Guid OrganizationId, string Code, Guid? CreatedBy) : DomainEvent;

public sealed record TemplateUpdatedDomainEvent(Guid TemplateId, Guid OrganizationId, Guid? UpdatedBy) : DomainEvent;

public sealed record TemplateVersionCreatedDomainEvent(
    Guid TemplateId,
    Guid TemplateVersionId,
    Guid OrganizationId,
    int VersionNumber,
    Guid? CreatedBy) : DomainEvent;

public sealed record TemplateValidatedDomainEvent(
    Guid TemplateId,
    Guid TemplateVersionId,
    Guid OrganizationId,
    bool IsValid) : DomainEvent;

public sealed record TemplateActivatedDomainEvent(
    Guid TemplateId,
    Guid TemplateVersionId,
    Guid OrganizationId,
    Guid? ActivatedBy) : DomainEvent;

public sealed record TemplateDeactivatedDomainEvent(Guid TemplateId, Guid OrganizationId, Guid? DeactivatedBy) : DomainEvent;

public sealed record TemplateArchivedDomainEvent(Guid TemplateId, Guid OrganizationId, Guid? ArchivedBy) : DomainEvent;
