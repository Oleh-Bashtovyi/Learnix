using System.Text.Json;
using Ardalis.Specification;
using Learnix.Application.AiChat.Constants;
using Learnix.Application.AiChat.Queries.GetLessonForAi;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Errors;
using Learnix.Application.Enrollments.Abstractions;
using Learnix.Application.Lessons.Abstractions;
using Learnix.Application.TestAttempts.Abstractions;
using Learnix.Domain.Entities;
using Learnix.Domain.Enums;
using Learnix.Domain.ValueObjects;

namespace Learnix.Application.UnitTests.AiChat.Queries.GetLessonForAi;

public class GetLessonForAiQueryHandlerTests
{
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly IEnrollmentRepository _enrollmentRepository = Substitute.For<IEnrollmentRepository>();
    private readonly ILessonRepository _lessonRepository = Substitute.For<ILessonRepository>();
    private readonly ITestAttemptRepository _attemptRepository = Substitute.For<ITestAttemptRepository>();
    private readonly GetLessonForAiQueryHandler _sut;

    private static readonly Guid StudentId = Guid.NewGuid();
    private static readonly Guid CourseId = Guid.NewGuid();
    private static readonly Guid LessonId = Guid.NewGuid();
    private static readonly Guid SectionId = Guid.NewGuid();

    public GetLessonForAiQueryHandlerTests()
    {
        _currentUser.UserId.Returns(StudentId);
        _sut = new GetLessonForAiQueryHandler(
            _currentUser, _enrollmentRepository, _lessonRepository, _attemptRepository);
    }

    private void SubmittedAttempts(int count) =>
        _attemptRepository
            .CountAsync(Arg.Any<ISpecification<TestAttempt>>(), Arg.Any<CancellationToken>())
            .Returns(count);

    private void OpenAttempt(bool exists) =>
        _attemptRepository
            .AnyAsync(Arg.Any<ISpecification<TestAttempt>>(), Arg.Any<CancellationToken>())
            .Returns(exists);

    private void Enrolled(bool value) =>
        _enrollmentRepository
            .AnyAsync(Arg.Any<ISpecification<Enrollment>>(), Arg.Any<CancellationToken>())
            .Returns(value);

    private void LessonInCourse(Lesson? lesson) =>
        _lessonRepository
            .GetVisibleLessonInCourseAsync(CourseId, LessonId, Arg.Any<CancellationToken>())
            .Returns(lesson);

    private Task<FluentResults.Result<LessonForAiDto>> Act() =>
        _sut.Handle(new GetLessonForAiQuery(CourseId, LessonId), CancellationToken.None);

    [Fact]
    public async Task Fails_with_forbidden_when_the_student_is_not_enrolled()
    {
        Enrolled(false);

        var result = await Act();

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().BeOfType<ForbiddenError>();
        await _lessonRepository.DidNotReceiveWithAnyArgs()
            .GetVisibleLessonInCourseAsync(default, default, default);
    }

    [Fact]
    public async Task Fails_with_not_found_when_the_lesson_is_hidden_or_from_another_course()
    {
        Enrolled(true);
        LessonInCourse(null);

        var result = await Act();

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().BeOfType<NotFoundError>();
    }

    [Fact]
    public async Task Video_lesson_exposes_no_content_and_says_why()
    {
        Enrolled(true);
        var video = VideoLesson.Create(SectionId, "Intro to Hooks", "videos/intro.mp4", "What we cover", 300);
        LessonInCourse(video);

        var dto = (await Act()).Value;

        dto.LessonType.Should().Be(LessonType.Video);
        dto.ContentAvailable.Should().BeFalse();
        dto.Content.Should().BeNull();
        dto.ContentUnavailableReason.Should().NotBeNullOrWhiteSpace();
        dto.Description.Should().Be("What we cover");
    }

    [Fact]
    public async Task Post_lesson_body_is_truncated_to_the_tool_limit()
    {
        Enrolled(true);
        var body = new string('x', AiChatToolLimits.LessonContentMaxLength + 500);
        LessonInCourse(PostLesson.Create(SectionId, "Closures", body));

        var dto = (await Act()).Value;

        dto.ContentAvailable.Should().BeTrue();
        dto.ContentTruncated.Should().BeTrue();
        dto.Content.Should().HaveLength(AiChatToolLimits.LessonContentMaxLength);
    }

    [Fact]
    public async Task Test_lesson_leaks_neither_questions_nor_answers()
    {
        Enrolled(true);
        SubmittedAttempts(0);
        LessonInCourse(BuildTest());

        var dto = (await Act()).Value;

        dto.ContentAvailable.Should().BeFalse();
        dto.Test.Should().NotBeNull();
        dto.Test!.QuestionCount.Should().Be(2);
        dto.Test.PassingThreshold.Should().Be(70);
        dto.Test.AttemptLimit.Should().Be(3);
        dto.Test.ReviewAvailable.Should().BeFalse();

        // The DTO is what the model receives. Nothing in it may hint at the answers.
        var payload = JsonSerializer.Serialize(dto);
        payload.Should().NotContain("Paris");
        payload.Should().NotContain("Berlin");
        payload.Should().NotContain("Capital of France");
        payload.Should().NotContain("mitochondria");
    }

    [Fact]
    public async Task Test_lesson_offers_the_review_once_an_attempt_is_submitted()
    {
        Enrolled(true);
        SubmittedAttempts(1);
        OpenAttempt(false);
        LessonInCourse(BuildTest());

        var dto = (await Act()).Value;

        dto.Test!.SubmittedAttempts.Should().Be(1);
        dto.Test.ReviewAvailable.Should().BeTrue();
        dto.ContentUnavailableReason.Should().Contain(ChatToolNames.GetMyTestReview);
    }

    [Fact]
    public async Task Test_lesson_withholds_the_review_while_an_attempt_is_open()
    {
        Enrolled(true);
        SubmittedAttempts(1);
        OpenAttempt(true);
        LessonInCourse(BuildTest());

        var dto = (await Act()).Value;

        dto.Test!.ReviewAvailable.Should().BeFalse();
    }

    [Fact]
    public async Task Video_and_post_lessons_never_ask_about_attempts()
    {
        Enrolled(true);
        LessonInCourse(PostLesson.Create(SectionId, "Closures", "body"));

        await Act();

        await _attemptRepository.DidNotReceiveWithAnyArgs()
            .CountAsync(default(ISpecification<TestAttempt>)!, default);
    }

    private static TestLesson BuildTest()
    {
        var test = TestLesson.Create(SectionId, "Checkpoint", "Covers lessons 1-3", 3, 60, 70);

        test.SetCurrentVersion(TestVersion.Create(test.Id,
        [
            new QuestionBlueprint(
                "Capital of France",
                QuestionType.SingleChoice,
                [
                    new QuestionOptionBlueprint("Paris", true),
                    new QuestionOptionBlueprint("Berlin", false),
                ],
                null),
            new QuestionBlueprint(
                "Powerhouse of the cell",
                QuestionType.TextInput,
                null,
                new TextAnswerBlueprint("mitochondria", true, false)),
        ]));

        return test;
    }
}
