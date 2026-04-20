using Learnix.Domain.Common;
using Learnix.Domain.Enums;

namespace Learnix.Domain.Entities;

public abstract class Lesson : BaseEntity
{
    protected Lesson() { }

    protected Lesson(Guid sectionId, string title, int order, LessonType lessonType)
    {
        SectionId = sectionId;
        Title = title;
        Order = order;
        LessonType = lessonType;
    }

    public Guid SectionId { get; private set; }
    public string Title { get; private set; } = null!;
    public int Order { get; private set; }
    public LessonType LessonType { get; private set; }

    internal void UpdateTitle(string title) => Title = title;
    internal void SetOrder(int order) => Order = order;
}
