using System.Text.Json;
using Learnix.Application.Common.Events;
using Learnix.Application.Common.Options;
using Learnix.Domain.Entities;
using Learnix.Domain.Events;
using Learnix.Infrastructure.Outbox;
using Learnix.Infrastructure.Outbox.EventHandlers.Users;
using Learnix.Infrastructure.Outbox.Payloads.Users;
using Learnix.Infrastructure.Persistence.EntityFramework;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Learnix.Infrastructure.UnitTests.Outbox.EventHandlers.Users;

/// <summary>
/// The only handler that builds a URL by hand rather than filling a template — a wrong separator or an
/// un-escaped character here breaks the reset link for every user with a '+' or '&amp;' in their email, and
/// nothing short of clicking it would ever catch that.
/// </summary>
public class PasswordResetRequestedOutboxHandlerTests
{
    private const string ClientBaseUrl = "https://learnix.dev";

    [Fact]
    public async Task Handle_BuildsAResetLink_WithTheTokenBase64UrlEncodedAndTheEmailEscaped()
    {
        await using var context = Context();
        var user = new User("ada+test@learnix.dev", "Ada", "Lovelace");
        context.Users.Add(user);
        await context.SaveChangesAsync();
        var sut = Handler(context);
        // '+', '/', '=' are all characters a raw Identity token can contain and a URL cannot.
        var domainEvent = new PasswordResetRequestedDomainEvent(
            user.Id, "ada+test@learnix.dev", "Ada", "tok en+/=");

        await sut.Handle(new DomainEventNotification<PasswordResetRequestedDomainEvent>(domainEvent), CancellationToken.None);
        await context.SaveChangesAsync();

        var message = await context.OutboxMessages.SingleAsync();
        var payload = JsonSerializer.Deserialize<SendPasswordResetEmailPayload>(message.Payload)!;
        payload.ResetLink.Should().Be(
            $"{ClientBaseUrl}/reset-password?email=ada%2Btest%40learnix.dev&token=dG9rIGVuKy89");
    }

    [Fact]
    public async Task Handle_WhenTheUserRowIsGone_StillSendsTheEmail_DefaultingLanguageToEnglish()
    {
        // Unlike the ban/deletion emails, this one must not silently drop: the token in the event is
        // already valid and the user is locked out until they use it, whether or not the lookup succeeds.
        await using var context = Context();
        var sut = Handler(context);
        var domainEvent = new PasswordResetRequestedDomainEvent(
            Guid.NewGuid(), "ghost@learnix.dev", "Ghost", "token");

        await sut.Handle(new DomainEventNotification<PasswordResetRequestedDomainEvent>(domainEvent), CancellationToken.None);
        await context.SaveChangesAsync();

        var message = await context.OutboxMessages.SingleAsync();
        var payload = JsonSerializer.Deserialize<SendPasswordResetEmailPayload>(message.Payload)!;
        payload.Language.Should().Be("en");
    }

    private static PasswordResetRequestedOutboxHandler Handler(ApplicationDbContext context) => new(
        new OutboxDbContextHolder { DbContext = context },
        Options.Create(new AppOptions { ClientBaseUrl = ClientBaseUrl }));

    private static ApplicationDbContext Context()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }
}
