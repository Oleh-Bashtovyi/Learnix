using Learnix.Domain.Common;

namespace Learnix.Domain.Entities;

public class CourseMessage : BaseEntity
{
    private CourseMessage() { }

    private CourseMessage(Guid conversationId, Guid senderId, string content)
    {
        ConversationId = conversationId;
        SenderId = senderId;
        Content = content;
    }

    public Guid ConversationId { get; private set; }
    public Guid SenderId { get; private set; }
    public string Content { get; private set; } = null!;

    public User? Sender { get; private set; }

    internal static CourseMessage Create(Guid conversationId, Guid senderId, string content)
        => new(conversationId, senderId, content);
}
