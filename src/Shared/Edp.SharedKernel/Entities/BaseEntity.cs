using System.ComponentModel.DataAnnotations.Schema;
using Edp.SharedKernel.Domain;

namespace Edp.SharedKernel.Entities;

public abstract class BaseEntity<TId>
{
    private readonly List<DomainEvent> _domainEvents = [];

    public TId Id { get; protected set; } = default!;

    [NotMapped]
    public IReadOnlyCollection<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void AddDomainEvent(DomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();
}
