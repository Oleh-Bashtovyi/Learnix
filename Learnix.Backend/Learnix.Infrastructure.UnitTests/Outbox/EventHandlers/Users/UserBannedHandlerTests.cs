using System.Text.Json;
using Learnix.Application.Common.Events;
using Learnix.Domain.Entities;
using Learnix.Domain.Events.User;
using Learnix.Infrastructure.Outbox;
using Learnix.Infrastructure.Outbox.EventHandlers.Users;
using Learnix.Infrastructure.Outbox.Payloads.Users;
using Learnix.Infrastructure.Persistence.EntityFramework;
using Microsoft.EntityFrameworkCore;

namespace Learnix.Infrastructure.UnitTests.Outbox.EventHandlers.Users;

/// <summary>
/// The ban email carries the user's own name and language, so — unlike a fire-and-forget notification —
/// this handler has to look the user back up from the row the ban was already applied to.
/// </summary>
public class UserBannedHandlerTests
{
    [Fact]
    public async Task Handle_EnqueuesTheBanEmail_WithTheUsersOwnEmailNameAndLanguage()
    {
        await using var context = Context();
        var user = new User("banned@learnix.dev", "Ada", "Lovelace");
        user.SetLanguage("uk");
        context.Users.Add(user);
        await context.SaveChangesAsync();
        var holder = new OutboxDbContextHolder { DbContext = context };
        var sut = new UserBannedHandler(holder);
        var domainEvent = new UserBannedDomainEvent(user.Id);

        await sut.Handle(new DomainEventNotification<UserBannedDomainEvent>(domainEvent), CancellationToken.None);
        await context.SaveChangesAsync();

        var message = await context.OutboxMessages.SingleAsync();
        message.Type.Should().Be(OutboxMessageTypes.UserBannedEmail);
        JsonSerializer.Deserialize<SendUserBannedEmailPayload>(message.Payload)
            .Should().Be(new SendUserBannedEmailPayload("banned@learnix.dev", "Ada", "uk"));
    }

    [Fact]
    public async Task Handle_WhenTheUserRowIsGone_EnqueuesNothing()
    {
        // Nothing to email and no address to send it to — silently skipping is the only option, not a bug
        // to work around.
        await using var context = Context();
        var holder = new OutboxDbContextHolder { DbContext = context };
        var sut = new UserBannedHandler(holder);
        var domainEvent = new UserBannedDomainEvent(Guid.NewGuid());

        await sut.Handle(new DomainEventNotification<UserBannedDomainEvent>(domainEvent), CancellationToken.None);
        await context.SaveChangesAsync();

        (await context.OutboxMessages.CountAsync()).Should().Be(0);
    }

    private static ApplicationDbContext Context()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }
}
