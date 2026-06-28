using Learnix.Infrastructure.Persistence.EntityFramework;

namespace Learnix.Infrastructure.Outbox;

// Scoped carrier — the DomainEventsInterceptor sets this before publishing events so that
// Infrastructure outbox handlers can write to the same DbContext that triggered SavingChangesAsync,
// keeping outbox messages in the same transaction as the entity changes.
internal sealed class OutboxDbContextHolder
{
    public ApplicationDbContext? DbContext { get; set; }
}
