using System.Text.Json;
using Ardalis.Specification;
using Learnix.Application.AiChat.Queries.GetTestReviewForAi;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Errors;
using Learnix.Application.Enrollments.Abstractions;
using Learnix.Application.Lessons.Abstractions;
using Learnix.Application.TestAttempts.Abstractions;
using Learnix.Domain.Entities;
using Learnix.Domain.Enums;
using Learnix.Domain.ValueObjects;

namespace Learnix.Application.UnitTests.AiChat.Queries.GetTestReviewForAi;

public class GetTestReviewForAiQueryHandlerTests
{
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly IEnrollmentRepository _enrollmentRepository = Substitute.For<IEnrollmentRepository>();
    private readonly ILessonRepository _lessonRepository = Substitute.For<ILessonRepository>();
    private readonly ITestVersionRepository _versionRepository = Substitute.For<ITestVersionRepository>();
    private readonly ITestAttemptRepository _attemptRepository = Substitute.For<ITestAttemptRepository>();
    private readonly GetTestReviewForAiQueryHandler _sut;

    private static readonly Guid StudentId = Guid.NewGuid();
    private static readonly Guid CourseId = Guid.NewGuid();
    private static readonly Guid LessonId = Guid.NewGuid();
    private static readonly Guid VersionId = Guid.NewGuid();
    private static readonly Guid SectionId = Guid.NewGuid();

    public GetTestReviewForAiQueryHandlerTests()
    {
        _currentUser.UserId.Returns(StudentId);
        _sut = new GetTestReviewForAiQueryHandler(
            _currentUser, _enrollmentRepository, _lessonRepository, _versionRepository, _attemptRepository);
    }

    private void Enrolled(bool value) =>
        _enrollmentRepository
            .AnyAsync(Arg.Any<ISpecification<Enrollment>>(), Arg.Any<CancellationToken>())
            .Returns(value);

    private void TestInCourse(TestLesson? test)
    {
        _lessonRepository
            .GetTestLessonInCourseAsync(CourseId, LessonId, Arg.Any<CancellationToken>())
            .Returns(test);

        _versionRepository
            .FirstOrDefaultAsync(Arg.Any<ISpecification<TestVersion>>(), Arg.Any<CancellationToken>())
            .Returns(test is null ? null : BuildVersion());
    }

    private void OpenAttempt(bool exists) =>
        _attemptRepository
            .AnyAsync(Arg.Any<ISpecification<TestAttempt>>(), Arg.Any<CancellationToken>())
            .Returns(exists);

    private void SubmittedAttempts(params TestAttempt[] attempts) =>
        _attemptRepository
            .ListAsync(Arg.Any<ISpecification<TestAttempt>>(), Arg.Any<CancellationToken>())
            .Returns(attempts.ToList());

    private Task<FluentResults.Result<TestReviewForAiDto>> Act() =>
        _sut.Handle(new GetTestReviewForAiQuery(CourseId, LessonId), CancellationToken.None);

    [Fact]
    public async Task Fails_with_forbidden_when_the_student_is_not_enrolled()
    {
        Enrolled(false);

        var result = await Act();

        result.Errors[0].Should().BeOfType<ForbiddenError>();
        await _attemptRepository.DidNotReceiveWithAnyArgs()
            .ListAsync(default(ISpecification<TestAttempt>)!, default);
    }

    [Fact]
    public async Task Fails_when_the_test_has_never_been_submitted()
    {
        Enrolled(true);
        TestInCourse(BuildTest());
        OpenAttempt(false);
        SubmittedAttempts();

        var result = await Act();

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().BeOfType<NotFoundError>();
    }

    [Fact]
    public async Task Refuses_while_an_attempt_is_still_open()
    {
        Enrolled(true);
        TestInCourse(BuildTest());
        OpenAttempt(true);

        var result = await Act();

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().BeOfType<ConflictError>();

        // Nothing is loaded, so nothing can leak into an attempt the student is still taking.
        await _attemptRepository.DidNotReceiveWithAnyArgs()
            .ListAsync(default(ISpecification<TestAttempt>)!, default);
    }

    [Fact]
    public async Task Reviews_the_latest_submitted_attempt_with_answers_and_correctness()
    {
        Enrolled(true);
        var test = BuildTest();
        TestInCourse(test);
        OpenAttempt(false);

        // Picked Berlin (wrong), typed "mitochondria" (right).
        SubmittedAttempts(BuildAttempt(
            new StudentAnswer(0, [1], null),
            new StudentAnswer(1, [], "mitochondria")));

        var dto = (await Act()).Value;

        dto.AttemptNumber.Should().Be(2);
        dto.Score.Should().Be(1);
        dto.Questions.Should().HaveCount(2);

        var choice = dto.Questions[0];
        choice.Answered.Should().BeTrue();
        choice.IsCorrect.Should().BeFalse();
        choice.StudentSelectedOptionOrders.Should().Equal(1);
        choice.Options!.Single(o => o.IsCorrect).Text.Should().Be("Paris");

        var text = dto.Questions[1];
        text.IsCorrect.Should().BeTrue();
        text.StudentTextAnswer.Should().Be("mitochondria");
        text.CorrectTextAnswer.Should().Be("mitochondria");

        // Now the answers are allowed to be in the payload — the platform revealed them on submit.
        JsonSerializer.Serialize(dto).Should().Contain("Paris");
    }

    [Fact]
    public async Task Marks_a_skipped_question_as_unanswered_rather_than_wrong_input()
    {
        Enrolled(true);
        TestInCourse(BuildTest());
        OpenAttempt(false);
        SubmittedAttempts(BuildAttempt(new StudentAnswer(0, [0], null)));

        var dto = (await Act()).Value;

        dto.Questions[1].Answered.Should().BeFalse();
        dto.Questions[1].IsCorrect.Should().BeFalse();
        dto.Questions[1].StudentTextAnswer.Should().BeNull();
        dto.Questions[0].IsCorrect.Should().BeTrue();
    }

    private static TestAttempt BuildAttempt(params StudentAnswer[] answers)
    {
        var attempt = TestAttempt.Create(CourseId, LessonId, VersionId, StudentId, attemptNumber: 2);
        var version = BuildVersion();
        attempt.Submit(answers, version.Score(answers), version.MaxScore, passingThreshold: 70);
        return attempt;
    }

    private static TestLesson BuildTest()
    {
        var test = TestLesson.Create(SectionId, "Checkpoint", "Covers lessons 1-3", 3, 60, 70);
        test.SetCurrentVersion(BuildVersion(test.Id));
        return test;
    }

    /// <summary>
    /// Always carries <see cref="VersionId"/>, so it is the version <see cref="BuildAttempt"/> pinned —
    /// the tutor is only allowed to read the questions the student actually answered.
    /// </summary>
    private static TestVersion BuildVersion(Guid? lessonId = null)
    {
        var version = TestVersion.Create(lessonId ?? LessonId,
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
        ]);

        typeof(Domain.Common.BaseEntity)
            .GetProperty(nameof(TestVersion.Id))!
            .GetSetMethod(nonPublic: true)!
            .Invoke(version, [VersionId]);

        return version;
    }
}
