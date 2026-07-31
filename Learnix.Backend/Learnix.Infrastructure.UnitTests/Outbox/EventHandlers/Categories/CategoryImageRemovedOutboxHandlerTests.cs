using System.Text.Json;
using Learnix.Application.Common.Events;
using Learnix.Domain.Events.Category;
using Learnix.Infrastructure.Outbox;
using Learnix.Infrastructure.Outbox.EventHandlers.Categories;
using Learnix.Infrastructure.Persistence.EntityFramework;
using Microsoft.EntityFrameworkCore;

namespace Learnix.Infrastructure.UnitTests.Outbox.EventHandlers.Categories;

/// <summary>
/// The thinnest of the outbox event handlers — a straight domain-event-to-<see cref="DeleteBlobPayload"/>
/// mapping through <c>SimpleOutboxHandler</c> — which is exactly why a wrong field name here would slip
/// past every compiler check and just quietly leak the blob.
/// </summary>
public class CategoryImageRemovedOutboxHandlerTests
{
    [Fact]
    public async Task Handle_EnqueuesADeleteBlobMessage_ForTheImageThatWasRemoved()
    {
        await using var context = Context();
        var holder = new OutboxDbContextHolder { DbContext = context };
        var sut = new CategoryImageRemovedOutboxHandler(holder);
        var domainEvent = new CategoryImageRemovedDomainEvent(Guid.NewGuid(), "categories/programming.png");

        await sut.Handle(new DomainEventNotification<CategoryImageRemovedDomainEvent>(domainEvent), CancellationToken.None);
        await context.SaveChangesAsync();

        var message = await context.OutboxMessages.SingleAsync();
        message.Type.Should().Be(OutboxMessageTypes.DeleteBlob);
        message.Id.Should().Be(domainEvent.EventId);
        JsonSerializer.Deserialize<DeleteBlobPayload>(message.Payload)
            .Should().Be(new DeleteBlobPayload("categories/programming.png"));
    }

    private static ApplicationDbContext Context()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }
}
