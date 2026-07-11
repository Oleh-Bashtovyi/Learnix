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

    // S1144: no code calls the setter — EF Core materializes the navigation.
#pragma warning disable S1144
    public User? Sender { get; private set; }
#pragma warning restore S1144

    internal static CourseMessage Create(Guid conversationId, Guid senderId, string content)
        => new(conversationId, senderId, content);
}
