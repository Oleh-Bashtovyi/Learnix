using Learnix.Application.Common.Events;
using Learnix.Domain.Entities;
using Learnix.Domain.Events.Category;
using Learnix.Infrastructure.Outbox;
using Learnix.Infrastructure.Persistence.EntityFramework;
using Learnix.Infrastructure.Persistence.EntityFramework.Interceptors;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Learnix.Infrastructure.UnitTests.Persistence.Interceptors;

/// <summary>
/// The sweep that turns domain events raised on entities into MediatR notifications. Everything downstream
/// — the outbox, achievement evaluation, notifications — only ever runs because this interceptor dispatched
/// the event that started it.
/// </summary>
public class DomainEventsInterceptorTests
{
    [Fact]
    public async Task SavingChanges_PublishesANotificationForEachRaisedEvent()
    {
        var publisher = Substitute.For<IPublisher>();
        await using var context = Context(publisher);
        var category = CategoryWithImage();

        context.Add(category);
        await context.SaveChangesAsync();
        publisher.ClearReceivedCalls();

        // SetImage on a category that already has one raises two events in a single save: the old image
        // released, the new one set.
        category.SetImage("categories/new.png");
        await context.SaveChangesAsync();

        await publisher.Received(1).Publish(
            Arg.Is<object>(n => n is DomainEventNotification<CategoryImageRemovedDomainEvent>),
            Arg.Any<CancellationToken>());
        await publisher.Received(1).Publish(
            Arg.Is<object>(n => n is DomainEventNotification<CategoryImageSetDomainEvent>),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SavingChanges_PublishesEventsFromEveryChangedEntity_NotJustTheFirstOne()
    {
        var publisher = Substitute.For<IPublisher>();
        await using var context = Context(publisher);
        var first = CategoryWithImage();
        var second = CategoryWithImage();
        context.AddRange(first, second);
        await context.SaveChangesAsync();
        first.ClearDomainEvents();
        second.ClearDomainEvents();

        first.PrepareForDeletion();
        second.PrepareForDeletion();
        await context.SaveChangesAsync();

        await publisher.Received(2).Publish(
            Arg.Is<object>(n => n is DomainEventNotification<CategoryImageRemovedDomainEvent>),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SavingChanges_ClearsDomainEvents_SoAFollowUpSaveDoesNotRepublishThem()
    {
        // The events are cleared before the publish loop runs, not after — otherwise a handler that
        // triggers another SaveChanges within the same unit of work would see (and redispatch) events
        // that are already on their way out. Nothing else clears them, so if this stopped happening the
        // very next SaveChanges — even with no new changes — would replay the same event.
        var publisher = Substitute.For<IPublisher>();
        await using var context = Context(publisher);
        var category = CategoryWithImage();
        context.Add(category);
        await context.SaveChangesAsync();

        category.PrepareForDeletion();
        await context.SaveChangesAsync();
        category.DomainEvents.Should().BeEmpty();
        publisher.ClearReceivedCalls();

        await context.SaveChangesAsync();

        await publisher.DidNotReceive().Publish(Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SavingChanges_GivesTheOutboxHolder_TheSameDbContextThatIsBeingSaved()
    {
        // Infrastructure event handlers write their outbox rows through this holder so they land in the
        // same transaction as the entity change — a separate context would let the two commit apart.
        var publisher = Substitute.For<IPublisher>();
        var holder = new OutboxDbContextHolder();
        await using var context = Context(publisher, holder);
        var category = CategoryWithImage();
        context.Add(category);

        await context.SaveChangesAsync();

        holder.DbContext.Should().BeSameAs(context);
    }

    private static Category CategoryWithImage()
    {
        var category = Category.Create("Programming", "programming");
        category.SetImage("categories/programming.png");
        return category;
    }

    private static ApplicationDbContext Context(IPublisher publisher, OutboxDbContextHolder? holder = null)
    {
        var services = new ServiceCollection();
        services.AddScoped(_ => publisher);
        services.AddSingleton(holder ?? new OutboxDbContextHolder());

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(new DomainEventsInterceptor(services.BuildServiceProvider()))
            .Options;

        return new ApplicationDbContext(options);
    }
}
