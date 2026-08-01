using Ardalis.Specification;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Storage;
using Learnix.Application.Common.Errors;
using Learnix.Application.Enrollments.Abstractions;
using Learnix.Application.Lessons.Abstractions;
using Learnix.Application.Lessons.Queries.GetLessonContent;
using Learnix.Domain.Entities;
using Learnix.Domain.Enums;
using Learnix.Domain.ValueObjects;

namespace Learnix.Application.UnitTests.Lessons.Queries.GetLessonContent;

public class GetLessonContentQueryHandlerTests
{
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly IEnrollmentRepository _enrollmentRepository = Substitute.For<IEnrollmentRepository>();
    private readonly ILessonRepository _lessonRepository = Substitute.For<ILessonRepository>();
    private readonly IBlobStorageService _blobStorage = Substitute.For<IBlobStorageService>();
    private readonly GetLessonContentQueryHandler _sut;

    private static readonly Guid StudentId = Guid.NewGuid();
    private static readonly Guid CourseId = Guid.NewGuid();
    private static readonly Guid LessonId = Guid.NewGuid();
    private static readonly Guid SectionId = Guid.NewGuid();

    public GetLessonContentQueryHandlerTests()
    {
        _currentUser.UserId.Returns(StudentId);
        _blobStorage.GenerateReadUrl(Arg.Any<string>(), Arg.Any<TimeSpan>()).Returns("https://blob/video.mp4?sas");
        _sut = new GetLessonContentQueryHandler(
            _currentUser, _enrollmentRepository, _lessonRepository, _blobStorage);
    }

    private void Enrolled(bool value) =>
        _enrollmentRepository
            .AnyAsync(Arg.Any<ISpecification<Enrollment>>(), Arg.Any<CancellationToken>())
            .Returns(value);

    private void LessonInCourse(Lesson? lesson) =>
        _lessonRepository
            .GetVisibleLessonInCourseAsync(CourseId, LessonId, Arg.Any<CancellationToken>())
            .Returns(lesson);

    private Task<FluentResults.Result<LessonContentDto>> Act() =>
        _sut.Handle(new GetLessonContentQuery(CourseId, LessonId), CancellationToken.None);

    /// <summary>The signed URL is minted per request and never stored — enrollment is what gates the video.</summary>
    [Fact]
    public async Task A_video_lesson_is_served_as_a_freshly_signed_url()
    {
        // Arrange
        Enrolled(true);
        LessonInCourse(VideoLesson.Create(SectionId, "Hooks", "videos/hooks.mp4", "What we cover", 300));

        // Act
        var dto = (await Act()).Value;

        // Assert
        dto.LessonType.Should().Be(LessonType.Video);
        dto.VideoUrl.Should().Be("https://blob/video.mp4?sas");
        dto.DurationSeconds.Should().Be(300);
        _blobStorage.Received(1).GenerateReadUrl("videos/hooks.mp4", Arg.Any<TimeSpan>());
    }

    [Fact]
    public async Task A_post_lesson_is_served_whole_and_asks_for_no_url()
    {
        // Arrange
        Enrolled(true);
        LessonInCourse(PostLesson.Create(SectionId, "Closures", "The whole body"));

        // Act
        var dto = (await Act()).Value;

        // Assert
        dto.Content.Should().Be("The whole body");
        dto.VideoUrl.Should().BeNull();
        _blobStorage.DidNotReceiveWithAnyArgs().GenerateReadUrl(default!, default);
    }

    /// <summary>
    /// The player fetches a test lesson through the same endpoint as any other. It must come back as rules
    /// only: the questions belong to the attempt flow, which enforces the attempt limit and the cooldown.
    /// </summary>
    [Fact]
    public async Task A_test_lesson_gives_up_no_questions_here()
    {
        Enrolled(true);
        var test = TestLesson.Create(SectionId, "Checkpoint", "Covers 1-3", 3, 60, 70);
        test.SetCurrentVersion(TestVersion.Create(test.Id,
        [
            new QuestionBlueprint(
                "Capital of France",
                QuestionType.SingleChoice,
                [new QuestionOptionBlueprint("Paris", true), new QuestionOptionBlueprint("Berlin", false)],
                null)
        ]));
        LessonInCourse(test);

        var dto = (await Act()).Value;

        dto.LessonType.Should().Be(LessonType.Test);
        dto.Content.Should().BeNull();
        dto.Description.Should().Be("Covers 1-3");

        var payload = System.Text.Json.JsonSerializer.Serialize(dto);
        payload.Should().NotContain("Paris");
        payload.Should().NotContain("Capital of France");
    }

    [Fact]
    public async Task A_student_who_is_not_enrolled_never_gets_as_far_as_the_lesson()
    {
        // Arrange
        Enrolled(false);

        // Act
        var result = await Act();

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().BeOfType<ForbiddenError>();
        await _lessonRepository.DidNotReceiveWithAnyArgs()
            .GetVisibleLessonInCourseAsync(default, default, default);
    }

    /// <summary>Hidden lessons and lessons of another course come back from the repository as null.</summary>
    [Fact]
    public async Task A_hidden_lesson_is_not_found()
    {
        // Arrange
        Enrolled(true);
        LessonInCourse(null);

        // Act
        var result = await Act();

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().BeOfType<NotFoundError>();
    }
}
