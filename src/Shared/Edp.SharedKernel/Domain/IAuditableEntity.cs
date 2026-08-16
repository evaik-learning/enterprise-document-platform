namespace Edp.SharedKernel.Domain;

public interface IAuditableEntity
{
    DateTimeOffset CreatedAt { get; }
    string? CreatedBy { get; }
    DateTimeOffset? ModifiedAt { get; }
    string? ModifiedBy { get; }
}
