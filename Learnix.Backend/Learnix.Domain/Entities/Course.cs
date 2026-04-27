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

    /// <summary>
    /// Setting cover to null on a Published course breaks the "must have cover" invariant.
    /// </summary>
    public void SetCoverImage(string? coverImageUrl)
    {
        CoverBlobPath = coverImageUrl;
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

    public void IncrementEnrollmentsCount() => EnrollmentsCount++;

    // Section structure (Course as aggregate root, see ADR-044)
    // =========================================================
    public bool SectionExists(Guid sectionId) => Sections.Any(s => s.Id == sectionId);
    
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
    }

    public void ReorderLessons(Guid sectionId, IReadOnlyList<(Guid Id, int Order)> pairs)
    {
        EnsureStructureMutable();

        var section = FindSection(sectionId);
        section.ReorderLessons(pairs);
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
