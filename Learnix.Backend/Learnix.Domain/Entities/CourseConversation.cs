using Learnix.Domain.Common;
using Learnix.Domain.Constants;

namespace Learnix.Domain.Entities;

public class CourseConversation : BaseEntity
{
    private CourseConversation() { }

    private CourseConversation(Guid courseId, Guid studentId, Guid instructorId)
    {
        CourseId = courseId;
        StudentId = studentId;
        InstructorId = instructorId;
    }

    public Guid CourseId { get; private set; }
    public Guid StudentId { get; private set; }
    public Guid InstructorId { get; private set; }
    public int StudentUnreadCount { get; private set; }
    public int InstructorUnreadCount { get; private set; }
    public string? LastMessagePreview { get; private set; }
    public DateTime? LastMessageAt { get; private set; }

    // S1144: no code calls these setters — EF Core materializes the navigations.
#pragma warning disable S1144
    public Course? Course { get; private set; }
    public User? Student { get; private set; }
    public User? Instructor { get; private set; }
#pragma warning restore S1144

    public static CourseConversation Create(Guid courseId, Guid studentId, Guid instructorId)
        => new(courseId, studentId, instructorId);

    public CourseMessage AddMessage(Guid senderId, string content)
    {
        LastMessageAt = DateTime.UtcNow;
        LastMessagePreview = content.Length > ConversationConstants.PreviewMaxLength
            ? string.Concat(content.AsSpan(0, ConversationConstants.PreviewMaxLength), "...")
            : content;

        if (senderId == StudentId)
            InstructorUnreadCount++;
        else
            StudentUnreadCount++;

        return CourseMessage.Create(Id, senderId, content);
    }

    public void MarkReadByStudent() => StudentUnreadCount = 0;
    public void MarkReadByInstructor() => InstructorUnreadCount = 0;
}
