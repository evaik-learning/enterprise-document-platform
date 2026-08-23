using Edp.SharedKernel.Domain;

namespace Edp.Document.Domain.Events;

public sealed record DocumentCreatedDomainEvent(Guid DocumentId, Guid OrganizationId, Guid TemplateId, string DocumentName) : DomainEvent;

public sealed record DocumentGeneratedDomainEvent(Guid DocumentId, Guid OrganizationId, int VersionNumber, Guid TemplateVersionId) : DomainEvent;

public sealed record DocumentArchivedDomainEvent(Guid DocumentId, Guid OrganizationId) : DomainEvent;
