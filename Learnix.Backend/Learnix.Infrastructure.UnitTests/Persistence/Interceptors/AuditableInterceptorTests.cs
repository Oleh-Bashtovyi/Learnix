using Learnix.Domain.Common;
using Learnix.Infrastructure.Persistence.EntityFramework.Interceptors;
using Microsoft.EntityFrameworkCore;

namespace Learnix.Infrastructure.UnitTests.Persistence.Interceptors;

/// <summary>
/// Every entity's CreatedAt/UpdatedAt is written here, not by the code that inserts or updates it — so
/// this is the only place a wrong or missing timestamp would ever be caught.
/// </summary>
public class AuditableInterceptorTests
{
    [Fact]
    public async Task InsertingAnEntity_StampsCreatedAtAndUpdatedAtToTheSameInstant()
    {
        await using var context = Context();
        var entity = new AuditableThing();

        context.Add(entity);
        await context.SaveChangesAsync();

        entity.CreatedAt.Should().NotBe(default);
        entity.UpdatedAt.Should().Be(entity.CreatedAt);
    }

    [Fact]
    public async Task UpdatingAnEntity_AdvancesUpdatedAtButLeavesCreatedAtAlone()
    {
        await using var context = Context();
        var entity = new AuditableThing();
        context.Add(entity);
        await context.SaveChangesAsync();
        var createdAt = entity.CreatedAt;

        // A real clock tick between the two saves is what makes "advances" observable rather than assumed.
        await Task.Delay(10);
        entity.Name = "renamed";
        await context.SaveChangesAsync();

        entity.CreatedAt.Should().Be(createdAt);
        entity.UpdatedAt.Should().BeAfter(createdAt);
    }

    [Fact]
    public async Task UntouchedEntity_KeepsItsOriginalTimestamps()
    {
        // A save triggered by something else in the change set must not sweep up entities nobody modified.
        await using var context = Context();
        var untouched = new AuditableThing();
        var other = new AuditableThing();
        context.AddRange(untouched, other);
        await context.SaveChangesAsync();
        var (createdAt, updatedAt) = (untouched.CreatedAt, untouched.UpdatedAt);

        await Task.Delay(10);
        other.Name = "renamed";
        await context.SaveChangesAsync();

        untouched.CreatedAt.Should().Be(createdAt);
        untouched.UpdatedAt.Should().Be(updatedAt);
    }

    private static TestDbContext Context()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(new AuditableInterceptor())
            .Options;

        return new TestDbContext(options);
    }

    /// <summary>A minimal IAuditable — the interceptor only ever looks at that interface, never the entity type.</summary>
    private sealed class AuditableThing : IAuditable
    {
        public Guid Id { get; init; } = Guid.NewGuid();
        public string Name { get; set; } = "original";
        public DateTime CreatedAt { get; private set; }
        public DateTime UpdatedAt { get; private set; }
    }

    private sealed class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
    {
        public DbSet<AuditableThing> Things => Set<AuditableThing>();
    }
}
