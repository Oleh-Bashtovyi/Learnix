using Learnix.Domain.Events.Lessons;

namespace Learnix.Infrastructure.Outbox.EventHandlers.Lessons;

internal sealed class LessonVideoReleasedHandler(OutboxDbContextHolder holder)
    : SimpleOutboxHandler<LessonVideoReleasedDomainEvent, DeleteBlobPayload>(holder)
{
    protected override string MessageType => OutboxMessageTypes.DeleteBlob;
    protected override DeleteBlobPayload BuildPayload(LessonVideoReleasedDomainEvent e)
        => new(e.ReleasedBlobPath);
}
