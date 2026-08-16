using Edp.SharedKernel.Domain;

namespace Edp.SharedKernel.Entities;

public abstract class AuditableEntity<TId> : BaseEntity<TId>, IAuditableEntity
{
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTimeOffset? ModifiedAt { get; set; }
    public string? ModifiedBy { get; set; }
}
