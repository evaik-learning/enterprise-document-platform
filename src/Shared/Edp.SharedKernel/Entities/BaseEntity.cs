namespace Edp.SharedKernel.Entities;

public abstract class BaseEntity<TId>
{
    public TId Id { get; protected set; } = default!;
}
