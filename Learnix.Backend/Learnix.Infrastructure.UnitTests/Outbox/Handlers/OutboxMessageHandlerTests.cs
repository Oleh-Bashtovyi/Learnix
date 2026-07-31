using System.Text.Json;
using Learnix.Infrastructure.Outbox.Handlers;

namespace Learnix.Infrastructure.UnitTests.Outbox.Handlers;

/// <summary>
/// Every concrete outbox handler's payload passes through this one deserialize step. The stored JSON was
/// written by this same assembly, so a payload that will not parse is a bug or a corrupted row — either
/// way this is what decides it reaches the processor's retry-and-log path instead of a silent null.
/// </summary>
public class OutboxMessageHandlerTests
{
    [Fact]
    public async Task HandleAsync_DeserializesThePayload_AndPassesItToTheTypedOverload()
    {
        var sut = new RecordingHandler();
        var payload = new Payload(Guid.NewGuid(), "Ada");

        await sut.HandleAsync(JsonSerializer.Serialize(payload), CancellationToken.None);

        sut.Handled.Should().Be(payload);
    }

    [Fact]
    public async Task HandleAsync_WhenThePayloadDeserializesToNull_ThrowsNamingTheMessageType()
    {
        var sut = new RecordingHandler();

        var act = () => sut.HandleAsync("null", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*TestPayload*");
    }

    private sealed record Payload(Guid Id, string Name);

    private sealed class RecordingHandler : OutboxMessageHandler<Payload>
    {
        public override string MessageType => "TestPayload";

        public Payload? Handled { get; private set; }

        protected override Task HandleAsync(Payload payload, CancellationToken cancellationToken)
        {
            Handled = payload;
            return Task.CompletedTask;
        }
    }
}
