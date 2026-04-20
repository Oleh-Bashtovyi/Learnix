using Learnix.Domain.Common;
using Learnix.Domain.Common.Exceptions;
using Learnix.Domain.Enums;
using Learnix.Domain.Events;

namespace Learnix.Domain.Entities;

public class Course : BaseEntity, ISoftDeletable
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
    public string? CoverImageUrl { get; private set; }
    public decimal Price { get; private set; }
    public CourseStatus Status { get; private set; }

    /// <summary>
    /// Denormalized counter. Update strategy TBD — see ADR-041.
    /// </summary>
    public int EnrollmentsCount { get; private set; }

    public List<string> Tags { get; private set; } = [];
    public IReadOnlyCollection<Section> Sections => _sections.AsReadOnly();

    public bool IsDeleted { get; private set; } = false;
    public DateTime? DeletedAt { get; private set; } = null;

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

    /// <summary>
    /// Setting cover to null on a Published course breaks the "must have cover" invariant.
    /// </summary>
    public void SetCoverImage(string? coverImageUrl)
    {
        CoverImageUrl = coverImageUrl;
        EnsurePublishableInvariants();
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

        RaiseDomainEvent(new CoursePublishedDomainEvent(Id));
    }

    public void Unpublish()
    {
        if (Status != CourseStatus.Published)
            return;

        Status = CourseStatus.Draft;
        RaiseDomainEvent(new CourseUnpublishedDomainEvent(Id));
    }

    public void Archive()
    {
        if (Status == CourseStatus.Archived)
            return;

        Status = CourseStatus.Archived;
        RaiseDomainEvent(new CourseArchivedDomainEvent(Id));
    }

    public void MarkForDeletion()
        => RaiseDomainEvent(new CourseDeletedDomainEvent(Id));

    // Section structure (Course as aggregate root, see ADR-044)
    // =========================================================
    public Section AddSection(string title)
    {
        EnsureStructureMutable();

        var order = _sections.Count == 0 ? 0 : _sections.Max(s => s.Order) + 1;
        var section = Section.Create(Id, title, order);
        _sections.Add(section);
        // Adding a section never breaks invariants → no post-check.
        return section;
    }

    public void UpdateSectionTitle(Guid sectionId, string title)
    {
        EnsureStructureMutable();
        FindSection(sectionId).UpdateTitle(title);
    }

    public void RemoveSection(Guid sectionId)
    {
        EnsureStructureMutable();

        var section = FindSection(sectionId);
        _sections.Remove(section);

        EnsurePublishableInvariants();
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
        // Reorder doesn't affect counts → no invariant check.
    }

    // Lesson structure
    // ========================
    public VideoLesson AddVideoLesson(
        Guid sectionId,
        string title,
        string videoUrl,
        string? description,
        int? durationSeconds)
    {
        EnsureStructureMutable();

        var section = FindSection(sectionId);
        var lesson = VideoLesson.Create(
            sectionId,
            title,
            section.NextLessonOrder(),
            videoUrl,
            description,
            durationSeconds);
        section.AddLesson(lesson);
        return lesson;
    }

    public PostLesson AddPostLesson(Guid sectionId, string title, string content)
    {
        EnsureStructureMutable();

        var section = FindSection(sectionId);
        var lesson = PostLesson.Create(sectionId, title, section.NextLessonOrder(), content);
        section.AddLesson(lesson);
        return lesson;
    }

    public void UpdateVideoLesson(
        Guid lessonId,
        string title,
        string videoUrl,
        string? description,
        int? durationSeconds)
    {
        EnsureStructureMutable();

        var (_, lesson) = FindLessonWithSection(lessonId);
        if (lesson is not VideoLesson video)
            throw new DomainException($"Lesson {lessonId} is not a video lesson.");

        video.UpdateVideo(title, videoUrl, description, durationSeconds);
    }

    public void UpdatePostLesson(Guid lessonId, string title, string content)
    {
        EnsureStructureMutable();

        var (_, lesson) = FindLessonWithSection(lessonId);
        if (lesson is not PostLesson post)
            throw new DomainException($"Lesson {lessonId} is not a post lesson.");

        post.UpdatePost(title, content);
    }

    public void RemoveLesson(Guid lessonId)
    {
        EnsureStructureMutable();

        var (section, _) = FindLessonWithSection(lessonId);
        section.RemoveLesson(lessonId);

        EnsurePublishableInvariants();
    }

    public void ReorderLessons(Guid sectionId, IReadOnlyList<(Guid Id, int Order)> pairs)
    {
        EnsureStructureMutable();

        var section = FindSection(sectionId);
        section.ReorderLessons(pairs);
        // Reorder doesn't affect counts → no invariant check.
    }

    // Internal helpers
    // ====================================
    private Section FindSection(Guid sectionId)
        => _sections.FirstOrDefault(s => s.Id == sectionId)
            ?? throw new DomainException($"Section {sectionId} not found in course {Id}.");

    private (Section Section, Lesson Lesson) FindLessonWithSection(Guid lessonId)
    {
        foreach (var section in _sections)
        {
            var lesson = section.Lessons.FirstOrDefault(l => l.Id == lessonId);
            if (lesson is not null)
                return (section, lesson);
        }
        throw new DomainException($"Lesson {lessonId} not found in course {Id}.");
    }

    /// <summary>
    /// Archived courses are read-only; any structural change is rejected (see ADR-040, ADR-045).
    /// Draft and Published allow mutations, but Published enforces invariants post-hoc via
    /// <see cref="EnsurePublishableInvariants"/>.
    /// </summary>
    private void EnsureStructureMutable()
    {
        if (Status == CourseStatus.Archived)
            throw new DomainException("Archived courses are read-only.");
    }

    /// <summary>
    /// Invariants that must always hold while <see cref="Status"/> is <see cref="CourseStatus.Published"/>:
    /// 1) CoverImageUrl is set
    /// 2) At least one section
    /// 3) At least one lesson across all sections
    ///
    /// Called after every mutation that could break these. Throws if violated.
    /// </summary>
    private void EnsurePublishableInvariants()
    {
        if (Status != CourseStatus.Published)
            return;

        if (string.IsNullOrWhiteSpace(CoverImageUrl))
            throw new DomainException("Published course must have a cover image.");

        if (_sections.Count == 0)
            throw new DomainException("Published course must have at least one section.");

        if (_sections.All(s => s.Lessons.Count == 0))
            throw new DomainException("Published course must have at least one lesson.");
    }
}
