using Learnix.Domain.Enums;

namespace Learnix.Domain.Entities;

public class PostLesson : Lesson
{
    private PostLesson() { }

    private PostLesson(Guid sectionId, string title, int order, string content)
        : base(sectionId, title, order, LessonType.Post)
    {
        Content = content;
    }

    public string Content { get; private set; } = null!;

    internal static PostLesson Create(Guid sectionId, string title, int order, string content)
        => new(sectionId, title, order, content);

    internal void UpdatePost(string title, string content)
    {
        UpdateTitle(title);
        Content = content;
    }
}
