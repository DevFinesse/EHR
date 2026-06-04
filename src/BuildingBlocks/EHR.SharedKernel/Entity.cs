namespace EHR.SharedKernel;

public abstract class Entity
{
    protected Entity(Guid id)
    {
        Id = id;
    }

    public Guid Id { get; }
    public DateTimeOffset CreatedAt { get; } = DateTimeOffset.UtcNow;
}
