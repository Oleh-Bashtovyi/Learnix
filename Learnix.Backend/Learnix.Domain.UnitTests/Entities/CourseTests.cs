using Learnix.Domain.Common.Exceptions;
using Learnix.Domain.Entities;
using Learnix.Domain.Enums;
using Learnix.Domain.Events.Course;
using Learnix.Domain.Events.Lessons;

namespace Learnix.Domain.UnitTests.Entities;

public class CourseTests
{
    private const string Cover = "course-covers/cover.jpg";

    private static Course Draft() => Course.Create(Guid.NewGuid(), Guid.NewGuid(), "Title", "Desc", 100);

    /// <summary>A draft that satisfies every publish invariant: cover + section + one visible lesson.</summary>
    private static Course PublishableDraft()
    {
        var course = Draft();
        course.SetCoverImage(Cover);
        var section = course.AddSection("Section 1");
        course.AddLesson(PostLesson.Create(section.Id, "Lesson 1", "content"));
        return course;
    }

    private static Course Published()
    {
        var course = PublishableDraft();
        course.Publish();
        course.ClearDomainEvents();
        return course;
    }

    // Creation
    // ========
    [Fact]
    public void Create_ShouldSetInitialStateAndRaiseEvent()
    {
        // Arrange
        var instructorId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();

        // Act
        var course = Course.Create(instructorId, categoryId, "Title", "Desc", 100);

        // Assert
        course.Status.Should().Be(CourseStatus.Draft);
        course.EnrollmentsCount.Should().Be(0);
        course.DomainEvents.Should().ContainSingle(e => e is CourseCreatedDomainEvent);
    }

    // Publish invariants (ADR-005, ADR-010)
    // ====================================
    [Fact]
    public void Publish_WhenNoCoverImage_ShouldThrowDomainException()
    {
        // Arrange
        var course = Draft();
        course.AddSection("Section 1");

        // Act
        var act = () => course.Publish();

        // Assert
        act.Should().Throw<DomainException>()
           .WithMessage("Published course must have a cover image.");
    }

    [Fact]
    public void Publish_WhenNoSections_ShouldThrowDomainException()
    {
        // Arrange
        var course = Draft();
        course.SetCoverImage(Cover);

        // Act
        var act = () => course.Publish();

        // Assert
        act.Should().Throw<DomainException>()
           .WithMessage("Published course must have at least one section.");
    }

    [Fact]
    public void Publish_WhenSectionHasOnlyHiddenLessons_ShouldThrowDomainException()
    {
        // Arrange — a video lesson starts hidden, so the course has a section but nothing visible
        var course = Draft();
        course.SetCoverImage(Cover);
        var section = course.AddSection("Section 1");
        course.AddLesson(VideoLesson.Create(section.Id, "Lesson 1", "course-videos/v.mp4"));

        // Act
        var act = () => course.Publish();

        // Assert
        act.Should().Throw<DomainException>()
           .WithMessage("Published course must have at least one visible lesson.");
    }

    [Fact]
    public void Publish_WhenInvariantsFail_ShouldRollBackStatusToDraft()
    {
        // Arrange
        var course = Draft();
        course.SetCoverImage(Cover);

        // Act
        var act = () => course.Publish();

        // Assert — Publish() flips Status before validating, so it must restore it on failure
        act.Should().Throw<DomainException>();
        course.Status.Should().Be(CourseStatus.Draft);
        course.DomainEvents.Should().NotContain(e => e is CoursePublishedDomainEvent);
    }

    [Fact]
    public void Publish_WhenAllInvariantsHold_ShouldSetPublishedAndRaiseEvent()
    {
        // Arrange
        var course = PublishableDraft();
        course.ClearDomainEvents();

        // Act
        course.Publish();

        // Assert
        course.Status.Should().Be(CourseStatus.Published);
        course.DomainEvents.Should().ContainSingle(e => e is CoursePublishedDomainEvent);
    }

    [Fact]
    public void Publish_WhenAlreadyPublished_ShouldBeIdempotent()
    {
        // Arrange
        var course = Published();

        // Act
        course.Publish();

        // Assert
        course.Status.Should().Be(CourseStatus.Published);
        course.DomainEvents.Should().BeEmpty();
    }

    // Lifecycle transitions
    // =====================
    [Fact]
    public void Unpublish_WhenPublished_ShouldReturnToDraftAndRaiseEvent()
    {
        // Arrange
        var course = Published();

        // Act
        course.Unpublish();

        // Assert
        course.Status.Should().Be(CourseStatus.Draft);
        course.DomainEvents.Should().ContainSingle(e => e is CourseUnpublishedDomainEvent);
    }

    [Fact]
    public void Unpublish_WhenDraft_ShouldBeNoOp()
    {
        // Arrange
        var course = PublishableDraft();
        course.ClearDomainEvents();

        // Act
        course.Unpublish();

        // Assert
        course.Status.Should().Be(CourseStatus.Draft);
        course.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Archive_WhenPublished_ShouldRecordThatItWasPublished()
    {
        // Arrange
        var course = Published();

        // Act
        course.Archive();

        // Assert — WasPublished drives downstream cleanup (catalog counters, notifications)
        course.Status.Should().Be(CourseStatus.Archived);
        course.DomainEvents.Should().ContainSingle(e => e is CourseArchivedDomainEvent)
              .Which.As<CourseArchivedDomainEvent>().WasPublished.Should().BeTrue();
    }

    [Fact]
    public void Archive_WhenDraft_ShouldRecordThatItWasNotPublished()
    {
        // Arrange
        var course = PublishableDraft();
        course.ClearDomainEvents();

        // Act
        course.Archive();

        // Assert
        course.DomainEvents.Should().ContainSingle(e => e is CourseArchivedDomainEvent)
              .Which.As<CourseArchivedDomainEvent>().WasPublished.Should().BeFalse();
    }

    [Fact]
    public void Archive_WhenAlreadyArchived_ShouldBeIdempotent()
    {
        // Arrange
        var course = Published();
        course.Archive();
        course.ClearDomainEvents();

        // Act
        course.Archive();

        // Assert
        course.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Unarchive_ShouldReturnToDraftRatherThanPublished()
    {
        // Arrange — unarchiving must never silently re-expose a course in the catalog
        var course = Published();
        course.Archive();
        course.ClearDomainEvents();

        // Act
        course.Unarchive();

        // Assert
        course.Status.Should().Be(CourseStatus.Draft);
        course.DomainEvents.Should().ContainSingle(e => e is CourseUnarchivedDomainEvent);
    }

    // UpdateDetails — category reassignment (ADR: category counter kept via domain event, not inline)
    // =====================================================================================
    [Fact]
    public void UpdateDetails_WhenPublishedAndCategoryChanges_ShouldRaiseCategoryChangedEvent()
    {
        // Arrange
        var course = Published();
        var oldCategoryId = course.CategoryId;
        var newCategoryId = Guid.NewGuid();

        // Act
        course.UpdateDetails(newCategoryId, "Title", "Desc", 100, []);

        // Assert
        course.CategoryId.Should().Be(newCategoryId);
        course.DomainEvents.Should().ContainSingle(e => e is CourseCategoryChangedDomainEvent)
              .Which.As<CourseCategoryChangedDomainEvent>().Should().BeEquivalentTo(new
              {
                  CourseId = course.Id,
                  OldCategoryId = oldCategoryId,
                  NewCategoryId = newCategoryId,
              });
    }

    [Fact]
    public void UpdateDetails_WhenPublishedAndCategoryUnchanged_ShouldNotRaiseEvent()
    {
        // Arrange
        var course = Published();
        var sameCategoryId = course.CategoryId;

        // Act
        course.UpdateDetails(sameCategoryId, "Title", "Desc", 100, []);

        // Assert
        course.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void UpdateDetails_WhenDraftAndCategoryChanges_ShouldNotRaiseEvent()
    {
        // Arrange — draft courses are never counted, so no counter to keep in sync
        var course = PublishableDraft();
        course.ClearDomainEvents();

        // Act
        course.UpdateDetails(Guid.NewGuid(), "Title", "Desc", 100, []);

        // Assert
        course.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void MarkForDeletion_WhenPublished_ShouldRecordThatItWasPublished()
    {
        // Arrange
        var course = Published();

        // Act
        course.MarkForDeletion();

        // Assert
        course.DomainEvents.Should().ContainSingle(e => e is CourseDeletedDomainEvent)
              .Which.As<CourseDeletedDomainEvent>().WasPublished.Should().BeTrue();
    }

    [Fact]
    public void AdminUnpublish_WhenNotPublished_ShouldBeNoOp()
    {
        // Arrange
        var course = PublishableDraft();
        course.ClearDomainEvents();

        // Act
        course.AdminUnpublish();

        // Assert
        course.DomainEvents.Should().BeEmpty();
    }

    // Archived courses are read-only (EnsureStructureMutable)
    // =======================================================
    [Fact]
    public void AddSection_WhenArchived_ShouldThrowDomainException()
    {
        // Arrange
        var course = Published();
        course.Archive();

        // Act
        var act = () => course.AddSection("Another");

        // Assert
        act.Should().Throw<DomainException>().WithMessage("Archived courses are read-only.");
    }

    [Fact]
    public void RemoveSection_WhenArchived_ShouldThrowDomainException()
    {
        // Arrange
        var course = Published();
        var sectionId = course.Sections.Single().Id;
        course.Archive();

        // Act
        var act = () => course.RemoveSection(sectionId);

        // Assert
        act.Should().Throw<DomainException>().WithMessage("Archived courses are read-only.");
    }

    [Fact]
    public void ReorderSections_WhenArchived_ShouldThrowDomainException()
    {
        // Arrange
        var course = Published();
        var sectionId = course.Sections.Single().Id;
        course.Archive();

        // Act
        var act = () => course.ReorderSections([(sectionId, 0)]);

        // Assert
        act.Should().Throw<DomainException>().WithMessage("Archived courses are read-only.");
    }

    // Cover image
    // ===========
    [Fact]
    public void SetCoverImage_WhenReplacingExisting_ShouldReleaseTheOldBlobAndAttachTheNew()
    {
        // Arrange
        var course = Draft();
        course.SetCoverImage("course-covers/old.jpg");
        course.ClearDomainEvents();

        // Act
        course.SetCoverImage("course-covers/new.jpg");

        // Assert — the removed event carries the OLD path so the outbox can delete it
        course.DomainEvents.Should().ContainSingle(e => e is CourseCoverRemovedDomainEvent)
              .Which.As<CourseCoverRemovedDomainEvent>().CoverBlobPath.Should().Be("course-covers/old.jpg");
        course.DomainEvents.Should().ContainSingle(e => e is CourseCoverSetDomainEvent)
              .Which.As<CourseCoverSetDomainEvent>().CoverBlobPath.Should().Be("course-covers/new.jpg");
    }

    [Fact]
    public void SetCoverImage_WhenValueIsUnchanged_ShouldRaiseNoEvents()
    {
        // Arrange — re-saving a form without touching the cover must not queue a blob deletion
        var course = Draft();
        course.SetCoverImage(Cover);
        course.ClearDomainEvents();

        // Act
        course.SetCoverImage(Cover);

        // Assert
        course.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void SetCoverImage_ToNull_WhenPublished_ShouldThrowDomainException()
    {
        // Arrange
        var course = Published();

        // Act
        var act = () => course.SetCoverImage(null);

        // Assert
        act.Should().Throw<DomainException>()
           .WithMessage("Published course must have a cover image.");
    }

    // Section structure
    // =================
    [Fact]
    public void AddSection_ShouldAppendWithNextDisplayOrder()
    {
        // Arrange
        var course = Draft();

        // Act
        var first = course.AddSection("First");
        var second = course.AddSection("Second");

        // Assert
        first.DisplayOrder.Should().Be(0);
        second.DisplayOrder.Should().Be(1);
        course.Sections.Should().HaveCount(2);
    }

    [Fact]
    public void RemoveSection_WhenPublishedAndItIsTheLastSection_ShouldThrowDomainException()
    {
        // Arrange
        var course = Published();
        var sectionId = course.Sections.Single().Id;

        // Act
        var act = () => course.RemoveSection(sectionId);

        // Assert
        act.Should().Throw<DomainException>()
           .WithMessage("Published course must have at least one section.");
    }

    [Fact]
    public void RemoveSection_WhenItHoldsTheLastVisibleLessonOfAPublishedCourse_ShouldThrowDomainException()
    {
        // Arrange — a second, empty section keeps the section-count invariant satisfied,
        // so only the visible-lesson invariant can catch this.
        var course = Published();
        var sectionWithLesson = course.Sections.Single();
        course.AddSection("Empty section");

        // Act
        var act = () => course.RemoveSection(sectionWithLesson.Id);

        // Assert
        act.Should().Throw<DomainException>()
           .WithMessage("Published course must have at least one visible lesson.");
    }

    [Fact]
    public void RemoveSection_WhenAnotherSectionStillHasAVisibleLesson_ShouldSucceed()
    {
        // Arrange
        var course = Published();
        var doomed = course.AddSection("Doomed");
        var survivor = course.Sections.Single(s => s.Id != doomed.Id);
        survivor.Lessons.Should().ContainSingle(l => !l.IsHidden);

        // Act
        course.RemoveSection(doomed.Id);

        // Assert
        course.Sections.Should().ContainSingle().Which.Id.Should().Be(survivor.Id);
    }

    [Fact]
    public void RemoveSection_OnADraft_ShouldNotEnforcePublishedInvariants()
    {
        // Arrange — a draft may be emptied completely while it is being built
        var course = PublishableDraft();
        var sectionId = course.Sections.Single().Id;

        // Act
        course.RemoveSection(sectionId);

        // Assert
        course.Sections.Should().BeEmpty();
    }

    [Fact]
    public void FindSection_WhenSectionBelongsToAnotherCourse_ShouldThrowDomainException()
    {
        // Arrange
        var course = Draft();

        // Act
        var act = () => course.FindSection(Guid.NewGuid());

        // Assert
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void SectionExists_ShouldReflectMembership()
    {
        // Arrange
        var course = Draft();
        var section = course.AddSection("Section 1");

        // Act & Assert
        course.SectionExists(section.Id).Should().BeTrue();
        course.SectionExists(Guid.NewGuid()).Should().BeFalse();
    }

    // Reordering (ADR-011: set-equality validation)
    // =============================================
    [Fact]
    public void ReorderSections_WhenPayloadIsComplete_ShouldApplyNewOrders()
    {
        // Arrange
        var course = Draft();
        var a = course.AddSection("A");
        var b = course.AddSection("B");

        // Act
        course.ReorderSections([(a.Id, 1), (b.Id, 0)]);

        // Assert
        a.DisplayOrder.Should().Be(1);
        b.DisplayOrder.Should().Be(0);
    }

    [Fact]
    public void ReorderSections_WhenPayloadOmitsASection_ShouldThrow()
    {
        // Arrange — a partial payload would leave duplicate DisplayOrder values behind
        var course = Draft();
        var a = course.AddSection("A");
        course.AddSection("B");

        // Act
        var act = () => course.ReorderSections([(a.Id, 0)]);

        // Assert
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void ReorderSections_WhenPayloadContainsAForeignSection_ShouldThrow()
    {
        // Arrange
        var course = Draft();
        var a = course.AddSection("A");

        // Act
        var act = () => course.ReorderSections([(a.Id, 0), (Guid.NewGuid(), 1)]);

        // Assert
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void ReorderSections_WhenOrdersAreDuplicated_ShouldThrow()
    {
        // Arrange
        var course = Draft();
        var a = course.AddSection("A");
        var b = course.AddSection("B");

        // Act
        var act = () => course.ReorderSections([(a.Id, 0), (b.Id, 0)]);

        // Assert
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void ReorderSections_WhenPayloadIsEmpty_ShouldThrow()
    {
        // Arrange
        var course = Draft();
        course.AddSection("A");

        // Act
        var act = () => course.ReorderSections([]);

        // Assert
        act.Should().Throw<DomainException>();
    }

    // Lesson mutations through the aggregate root (ADR-009, ADR-017)
    // ==============================================================
    [Fact]
    public void RemoveLesson_WhenItIsTheLastVisibleLessonOfAPublishedCourse_ShouldThrowDomainException()
    {
        // Arrange
        var course = Published();
        var lesson = course.Sections.Single().Lessons.Single();

        // Act
        var act = () => course.RemoveLesson(lesson);

        // Assert
        act.Should().Throw<DomainException>()
           .WithMessage("Published course must have at least one visible lesson.");
    }

    [Fact]
    public void RemoveLesson_WhenLessonBelongsToAnotherCourse_ShouldThrowDomainException()
    {
        // Arrange
        var course = Draft();
        course.AddSection("Section 1");
        var foreign = PostLesson.Create(Guid.NewGuid(), "Foreign", "content");

        // Act
        var act = () => course.RemoveLesson(foreign);

        // Assert
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void RemoveLesson_OfAVideoLesson_ShouldReleaseItsBlob()
    {
        // Arrange — the removed video must be queued for deletion, not orphaned in blob storage
        var course = Draft();
        var section = course.AddSection("Section 1");
        var video = VideoLesson.Create(section.Id, "Lesson", "course-videos/v.mp4");
        course.AddLesson(video);
        course.ClearDomainEvents();
        video.ClearDomainEvents();

        // Act
        course.RemoveLesson(video);

        // Assert
        video.DomainEvents.Should().ContainSingle(e => e is LessonVideoReleasedDomainEvent)
             .Which.As<LessonVideoReleasedDomainEvent>().ReleasedBlobPath.Should().Be("course-videos/v.mp4");
        course.Sections.Single().Lessons.Should().BeEmpty();
    }

    [Fact]
    public void ToggleLessonVisibility_WhenHidingTheLastVisibleLessonOfAPublishedCourse_ShouldThrowDomainException()
    {
        // Arrange
        var course = Published();
        var lesson = course.Sections.Single().Lessons.Single();

        // Act
        var act = () => course.ToggleLessonVisibility(lesson, isVisible: false);

        // Assert
        act.Should().Throw<DomainException>()
           .WithMessage("Published course must have at least one visible lesson.");
    }

    [Fact]
    public void ToggleLessonVisibility_WhenLessonIsNotPublishReady_ShouldThrowDomainException()
    {
        // Arrange — a video lesson without a video can never be shown to students
        var course = Draft();
        var section = course.AddSection("Section 1");
        var video = VideoLesson.Create(section.Id, "Lesson", videoBlobPath: "");
        course.AddLesson(video);

        // Act
        var act = () => course.ToggleLessonVisibility(video, isVisible: true);

        // Assert
        act.Should().Throw<DomainException>().WithMessage("Cannot make this lesson visible");
    }

    [Fact]
    public void ToggleLessonVisibility_OnADraft_ShouldNotEnforcePublishedInvariants()
    {
        // Arrange — a draft may legitimately have zero visible lessons while it is being built
        var course = PublishableDraft();
        var lesson = course.Sections.Single().Lessons.Single();

        // Act
        course.ToggleLessonVisibility(lesson, isVisible: false);

        // Assert
        lesson.IsHidden.Should().BeTrue();
    }

    [Fact]
    public void HasLesson_ShouldSearchAcrossAllSections()
    {
        // Arrange
        var course = Draft();
        course.AddSection("Empty");
        var section = course.AddSection("Section 2");
        var lesson = PostLesson.Create(section.Id, "Lesson", "content");
        course.AddLesson(lesson);

        // Act & Assert
        course.HasLesson(lesson.Id).Should().BeTrue();
        course.TryGetLesson(lesson.Id).Should().BeSameAs(lesson);
        course.TryGetLesson(Guid.NewGuid()).Should().BeNull();
    }

    // Counters
    // ========
    [Fact]
    public void IncrementEnrollmentsCount_ShouldIncreaseByOne()
    {
        // Arrange
        var course = Draft();

        // Act
        course.IncrementEnrollmentsCount();
        course.IncrementEnrollmentsCount();

        // Assert
        course.EnrollmentsCount.Should().Be(2);
    }

    [Fact]
    public void SyncRating_ShouldOverwriteRatingAggregates()
    {
        // Arrange — the evaluator reconciles by SET (not increment) so retries stay idempotent
        var course = Draft();

        // Act
        course.SyncRating(reviewsCount: 3, averageRating: 4.5m);
        course.SyncRating(reviewsCount: 3, averageRating: 4.5m);

        // Assert
        course.ReviewsCount.Should().Be(3);
        course.AverageRating.Should().Be(4.5m);
    }
}
