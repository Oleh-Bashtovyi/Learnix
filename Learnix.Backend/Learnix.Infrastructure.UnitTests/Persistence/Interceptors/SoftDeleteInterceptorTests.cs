using Learnix.Domain.Common;
using Learnix.Infrastructure.Persistence.EntityFramework.Interceptors;
using Microsoft.EntityFrameworkCore;

namespace Learnix.Infrastructure.UnitTests.Persistence.Interceptors;

/// <summary>
/// The interceptor's own contract, isolated from the cascade-rescue behavior <see cref="SoftDeleteCascadeTests"/>
/// covers: a delete against an <see cref="ISoftDeletable"/> entity must turn into a flagged update, and the
/// interceptor must leave everything else to EF's real DELETE.
/// </summary>
public class SoftDeleteInterceptorTests
{
    [Fact]
    public async Task DeletingASoftDeletableEntity_TurnsIntoAnUpdateThatFlagsIt()
    {
        await using var context = Context();
        var entity = new SoftDeletableThing();
        context.Add(entity);
        await context.SaveChangesAsync();

        context.Remove(entity);
        await context.SaveChangesAsync();

        var saved = await context.SoftDeletables.IgnoreQueryFilters().SingleAsync();
        saved.IsDeleted.Should().BeTrue();
        saved.DeletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task DeletingAnEntityThatIsNotSoftDeletable_ActuallyRemovesItsRow()
    {
        // The interceptor only ever iterates Entries<ISoftDeletable>() — anything else must fall straight
        // through to a real DELETE, or every hard-delete in the codebase would silently stop deleting.
        await using var context = Context();
        var entity = new PlainThing();
        context.Add(entity);
        await context.SaveChangesAsync();

        context.Remove(entity);
        await context.SaveChangesAsync();

        (await context.PlainThings.IgnoreQueryFilters().CountAsync()).Should().Be(0);
    }

    private static TestDbContext Context()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(new SoftDeleteInterceptor())
            .Options;

        return new TestDbContext(options);
    }

    private sealed class SoftDeletableThing : ISoftDeletable
    {
        public Guid Id { get; init; } = Guid.NewGuid();
        public bool IsDeleted { get; private set; }
        public DateTime? DeletedAt { get; private set; }
    }

    private sealed class PlainThing
    {
        public Guid Id { get; init; } = Guid.NewGuid();
    }

    private sealed class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
    {
        public DbSet<SoftDeletableThing> SoftDeletables => Set<SoftDeletableThing>();
        public DbSet<PlainThing> PlainThings => Set<PlainThing>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<SoftDeletableThing>().HasQueryFilter(e => !e.IsDeleted);
        }
    }
}
