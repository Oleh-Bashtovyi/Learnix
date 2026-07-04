namespace Learnix.Domain.Common;

public abstract class SoftDeletableEntity : BaseEntity, ISoftDeletable
{
    public bool IsDeleted { get; private set; } = false;
    public DateTime? DeletedAt { get; private set; } = null;

    public void Delete()
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
    }

    public void Recover()
    {
        IsDeleted = false;
        DeletedAt = null;
    }
}
