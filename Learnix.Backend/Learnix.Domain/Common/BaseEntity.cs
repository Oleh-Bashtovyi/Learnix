namespace Learnix.Domain.Common;

public abstract class BaseEntity : IAuditable, IHasDomainEvents
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public Guid Id { get; protected set; } = Guid.NewGuid();
    
    #pragma warning disable S1144
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    #pragma warning restore S1144

    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void RaiseDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();

    /// <summary>
    /// Hook for releasing external resources (blobs, etc.) via domain events
    /// before the entity is removed. Called by delete handlers before dbContext.Remove().
    /// Override in subclasses that own external resources.
    /// </summary>
    public virtual void PrepareForDeletion() { }
}
