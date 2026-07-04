using Learnix.Domain.Common;
using Learnix.Domain.Common.Exceptions;
using Learnix.Domain.Enums;
using Learnix.Domain.Events.Course;

namespace Learnix.Domain.Entities;

public class Course : SoftDeletableEntity
{
    private readonly List<Section> _sections = [];

    private Course() { }

    private Course(
        Guid instructorId,
        Guid categoryId,
        string title,
        string description,
        decimal price,
        IEnumerable<string>? tags)
    {
        InstructorId = instructorId;
        CategoryId = categoryId;
        Title = title;
        Description = description;
        Price = price;
        Status = CourseStatus.Draft;
        EnrollmentsCount = 0;
        Tags = tags?.ToList() ?? [];

        RaiseDomainEvent(new CourseCreatedDomainEvent(Id, instructorId, categoryId));
    }

    public Guid InstructorId { get; private set; }
    public Guid CategoryId { get; private set; }
    public string Title { get; private set; } = null!;
    public string Description { get; private set; } = null!;
    public string? CoverBlobPath { get; private set; }
    public decimal Price { get; private set; }
    public CourseStatus Status { get; private set; }

    /// <summary>
    /// Denormalized counter. Update strategy TBD — see ADR-041.
    /// </summary>
    public int EnrollmentsCount { get; private set; }

    public decimal AverageRating { get; private set; }
    public int ReviewsCount { get; private set; }

    public List<string> Tags { get; private set; } = [];
    public IReadOnlyCollection<Section> Sections => _sections.AsReadOnly();

    public static Course Create(
        Guid instructorId,
        Guid categoryId,
        string title,
        string description,
        decimal price,
        IEnumerable<string>? tags = null)
        => new(instructorId, categoryId, title, description, price, tags);

    // Course-level details
    // ====================
    public void UpdateDetails(
        Guid categoryId,
        string title,
        string description,
        decimal price,
        IEnumerable<string> tags)
    {
        CategoryId = categoryId;
        Title = title;
        Description = description;
        Price = price;
        Tags = tags.ToList();
    }

    public void SetCoverImage(string? coverImageUrl)
    {
        if (CoverBlobPath == coverImageUrl)
            return;

        if (CoverBlobPath is not null)
            RaiseDomainEvent(new CourseCoverRemovedDomainEvent(Id, CoverBlobPath));

        CoverBlobPath = coverImageUrl;

        if (CoverBlobPath is not null)
            RaiseDomainEvent(new CourseCoverSetDomainEvent(Id, CoverBlobPath));

        if (Status == CourseStatus.Published && string.IsNullOrWhiteSpace(CoverBlobPath))
            throw new DomainException("Published course must have a cover image.");
    }

    // Lifecycle
    // ===========================
    public void Publish()
    {
        if (Status == CourseStatus.Published)
            return;

        // All invariants go through the shared check. If any fail → throw.
        Status = CourseStatus.Published;
        try
        {
            EnsurePublishableInvariants();
        }
        catch
        {
            Status = CourseStatus.Draft; // rollback in-memory
            throw;
        }

        RaiseDomainEvent(new CoursePublishedDomainEvent(Id, CategoryId));
    }

    public void Unpublish()
    {
        if (Status != CourseStatus.Published)
            return;

        Status = CourseStatus.Draft;
        RaiseDomainEvent(new CourseUnpublishedDomainEvent(Id, CategoryId));
    }

    public void Archive()
    {
        if (Status == CourseStatus.Archived)
            return;

        var wasPublished = Status == CourseStatus.Published;
        Status = CourseStatus.Archived;
        RaiseDomainEvent(new CourseArchivedDomainEvent(Id, CategoryId, wasPublished));
    }

    public void Unarchive()
    {
        if (Status != CourseStatus.Archived)
            return;

        Status = CourseStatus.Draft;
        RaiseDomainEvent(new CourseUnarchivedDomainEvent(Id, CategoryId));
    }

    public void MarkForDeletion()
        => RaiseDomainEvent(new CourseDeletedDomainEvent(Id, CategoryId, Status == CourseStatus.Published));

    public void AdminUnpublish()
    {
        if (Status != CourseStatus.Published)
            return;

        Status = CourseStatus.Draft;
        RaiseDomainEvent(new CourseAdminUnpublishedDomainEvent(Id, InstructorId, CategoryId));
    }

    public void AdminDelete()
        => RaiseDomainEvent(new CourseAdminDeletedDomainEvent(Id, InstructorId, CategoryId, Status == CourseStatus.Published));

    public void IncrementEnrollmentsCount() => EnrollmentsCount++;

    public void SyncRating(int reviewsCount, decimal averageRating)
    {
        ReviewsCount = reviewsCount;
        AverageRating = averageRating;
    }

    // Section structure (Course as aggregate root, see ADR-044)
    // =========================================================
    public bool SectionExists(Guid sectionId) => Sections.Any(s => s.Id == sectionId);

    public bool HasLesson(Guid lessonId) => Sections.Any(s => s.Lessons.Any(l => l.Id == lessonId));

    public Lesson? TryGetLesson(Guid lessonId) => Sections.SelectMany(s => s.Lessons).FirstOrDefault(l => l.Id == lessonId);

    public Section AddSection(string title)
    {
        EnsureStructureMutable();

        var order = _sections.Count == 0 ? 0 : _sections.Max(s => s.DisplayOrder) + 1;
        var section = Section.Create(Id, title, order);
        _sections.Add(section);
        return section;
    }

    public void RemoveSection(Guid sectionId)
    {
        EnsureStructureMutable();

        var section = FindSection(sectionId);
        _sections.Remove(section);

        if (Status == CourseStatus.Published && _sections.Count == 0)
            throw new DomainException("Published course must have at least one section.");
    }

    public void ReorderSections(IReadOnlyList<(Guid Id, int Order)> pairs)
    {
        EnsureStructureMutable();

        ReorderValidation.EnsureValid(
            pairs,
            existingIds: _sections.Select(s => s.Id),
            entityName: "section");

        var byId = _sections.ToDictionary(s => s.Id);

        foreach (var (id, order) in pairs)
            byId[id].SetOrder(order);
    }

    public void ReorderLessons(Guid sectionId, IReadOnlyList<(Guid Id, int Order)> pairs)
    {
        EnsureStructureMutable();

        var section = FindSection(sectionId);
        section.ReorderLessons(pairs);
    }

    public void RemoveLesson(Lesson lesson)
    {
        EnsureStructureMutable();

        var section = _sections.FirstOrDefault(s => s.Id == lesson.SectionId)
            ?? throw new DomainException($"Lesson {lesson.Id} does not belong to course {Id}.");

        lesson.PrepareForDeletion();

        section.RemoveLesson(lesson.Id);

        EnsurePublishableInvariants();
    }

    public void ToggleLessonVisibility(Lesson lesson, bool isVisible)
    {
        EnsureStructureMutable();

        var section = _sections.FirstOrDefault(s => s.Id == lesson.SectionId)
            ?? throw new DomainException($"Lesson {lesson.Id} does not belong to course {Id}.");

        lesson.SetVisibility(isVisible);

        EnsurePublishableInvariants();
    }

    // Internal helpers
    // ====================================
    public void EnsureStructureMutable()
    {
        if (Status == CourseStatus.Archived)
            throw new DomainException("Archived courses are read-only.");
    }

    public Section FindSection(Guid sectionId)
    => _sections.FirstOrDefault(s => s.Id == sectionId)
        ?? throw new DomainException($"Section {sectionId} not found in course {Id}.");


    /// <summary>
    /// Invariants that must always hold while <see cref="Status"/> is <see cref="CourseStatus.Published"/>:
    /// 1) CoverImageUrl is set
    /// 2) At least one section
    /// 3) At least one VISIBLE lesson across all sections
    ///
    /// Called after every mutation that could break these. Throws if violated.
    /// </summary>
    private void EnsurePublishableInvariants()
    {
        if (Status != CourseStatus.Published)
            return;

        if (string.IsNullOrWhiteSpace(CoverBlobPath))
            throw new DomainException("Published course must have a cover image.");

        if (_sections.Count == 0)
            throw new DomainException("Published course must have at least one section.");

        if (!_sections.Any(s => s.Lessons.Any(l => !l.IsHidden)))
            throw new DomainException("Published course must have at least one visible lesson.");
    }
}
