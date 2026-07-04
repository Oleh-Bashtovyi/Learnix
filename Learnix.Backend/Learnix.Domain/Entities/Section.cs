using Learnix.Domain.Common;
using Learnix.Domain.Common.Exceptions;

namespace Learnix.Domain.Entities;

public class Section : BaseEntity, IOrderable
{
    private readonly List<Lesson> _lessons = [];

    private Section() { }

    private Section(Guid courseId, string title, int order)
    {
        CourseId = courseId;
        Title = title;
        DisplayOrder = order;
    }

    public Guid CourseId { get; private set; }
    public string Title { get; private set; } = null!;
    public int DisplayOrder { get; private set; }

    public IReadOnlyCollection<Lesson> Lessons => _lessons.AsReadOnly();

    internal static Section Create(Guid courseId, string title, int order)
        => new(courseId, title, order);

    // All mutators are internal — only Course (same assembly) can orchestrate changes.
    // This keeps Course as the single entry point for structure mutations.

    public void UpdateTitle(string title) => Title = title;

    internal void SetOrder(int order) => DisplayOrder = order;

    internal void AddLesson(Lesson lesson) => _lessons.Add(lesson);

    internal void RemoveLesson(Guid lessonId)
    {
        var lesson = _lessons.FirstOrDefault(l => l.Id == lessonId)
            ?? throw new DomainException($"Lesson {lessonId} not found in section {Id}.");
        _lessons.Remove(lesson);
    }

    internal Lesson FindLesson(Guid lessonId)
        => _lessons.FirstOrDefault(l => l.Id == lessonId)
            ?? throw new DomainException($"Lesson {lessonId} not found in section {Id}.");

    internal int NextLessonOrder()
        => _lessons.Count == 0 ? 0 : _lessons.Max(l => l.DisplayOrder) + 1;

    internal void ReorderLessons(IReadOnlyList<(Guid Id, int Order)> pairs)
    {
        ReorderValidation.EnsureValid(
            pairs,
            existingIds: _lessons.Select(l => l.Id),
            entityName: "lesson");

        var byId = _lessons.ToDictionary(l => l.Id);
        foreach (var (id, order) in pairs)
            byId[id].SetOrder(order);
    }
}
