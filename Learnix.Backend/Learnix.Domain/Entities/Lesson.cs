using Learnix.Domain.Common;
using Learnix.Domain.Common.Exceptions;
using Learnix.Domain.Enums;

namespace Learnix.Domain.Entities;

public abstract class Lesson : BaseEntity, IOrderable
{
    protected Lesson() { }

    protected Lesson(Guid sectionId, string title, int order, LessonType lessonType)
    {
        SectionId = sectionId;
        Title = title;
        DisplayOrder = order;
        LessonType = lessonType;
    }

    public Guid SectionId { get; private set; }
    public string Title { get; private set; } = null!;
    public int DisplayOrder { get; private set; }
    public bool IsHidden { get; protected set; } = true;
    public LessonType LessonType { get; private set; }

    internal void UpdateTitle(string title) => Title = title;
    internal void SetOrder(int order) => DisplayOrder = order;
    public abstract bool IsPublishReady();

    internal void SetVisibility(bool isVisible)
    {
        if (isVisible && !IsPublishReady())
            throw new DomainException("Cannot make this lesson visible");

        IsHidden = !isVisible;
    }

    protected void EvaluateVisibility()
    {
        if (!IsPublishReady())
        {
            IsHidden = true;
        }
    }
}
