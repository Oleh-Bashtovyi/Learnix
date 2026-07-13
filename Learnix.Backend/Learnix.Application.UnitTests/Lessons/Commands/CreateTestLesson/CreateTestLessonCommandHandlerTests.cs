using Ardalis.Specification;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Errors;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.Lessons.Commands.CreateTestLesson;
using Learnix.Domain.Entities;
using Learnix.Domain.Enums;
using Learnix.Domain.ValueObjects;

namespace Learnix.Application.UnitTests.Lessons.Commands.CreateTestLesson;

public class CreateTestLessonCommandHandlerTests
{
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly ICourseRepository _courseRepository = Substitute.For<ICourseRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly CreateTestLessonCommandHandler _sut;

    private static readonly Guid InstructorId = Guid.NewGuid();

    private readonly Course _course = Course.Create(InstructorId, Guid.NewGuid(), "React", "Learn React", 49m);
    private readonly Guid _sectionId;

    public CreateTestLessonCommandHandlerTests()
    {
        _sut = new CreateTestLessonCommandHandler(_courseRepository, _unitOfWork, _currentUser);

        _sectionId = _course.AddSection("Section 1").Id;
        _currentUser.UserId.Returns(InstructorId);
        StubCourse(_course);
    }

    // Guards

    [Fact]
    public async Task Handle_WhenNotAuthenticated_ShouldReturnAuthenticationError()
    {
        _currentUser.UserId.Returns((Guid?)null);

        var result = await _sut.Handle(Command(), default);

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle().Which.Should().BeOfType<AuthenticationError>();
    }

    [Fact]
    public async Task Handle_WhenCourseDoesNotExist_ShouldReturnNotFound()
    {
        StubCourse(null);

        var result = await _sut.Handle(Command(), default);

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle().Which.Should().BeOfType<NotFoundError>();
    }

    [Fact]
    public async Task Handle_WhenSectionDoesNotExist_ShouldReturnNotFound()
    {
        var result = await _sut.Handle(Command(sectionId: Guid.NewGuid()), default);

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle().Which.Should().BeOfType<NotFoundError>();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // Happy path

    [Fact]
    public async Task Handle_WhenValid_ShouldAddTheTestLessonWithItsQuestionsAndReturnItsId()
    {
        var result = await _sut.Handle(Command(title: "Checkpoint", passingThreshold: 70), default);

        result.IsSuccess.Should().BeTrue();

        var lesson = _course.Sections.Single().Lessons.Should().ContainSingle().Subject;
        lesson.Should().BeOfType<TestLesson>();
        lesson.Id.Should().Be(result.Value);
        lesson.LessonType.Should().Be(LessonType.Test);

        var test = lesson.As<TestLesson>();
        test.Title.Should().Be("Checkpoint");
        test.PassingThreshold.Should().Be(70);
        test.Questions.Should().ContainSingle();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldAppendTheLessonAfterExistingOnes_UsingTheNextDisplayOrder()
    {
        // Arrange — the section already has a lesson at order 0
        _course.AddLesson(PostLesson.Create(_sectionId, "Existing", "content"));

        // Act
        var result = await _sut.Handle(Command(), default);

        // Assert
        var added = _course.Sections.Single().Lessons.Single(l => l.Id == result.Value);
        added.DisplayOrder.Should().Be(1);
    }

    // Fixtures

    private CreateTestLessonCommand Command(
        Guid? sectionId = null,
        string title = "Quiz",
        int passingThreshold = 70,
        TestReviewMode reviewMode = TestReviewMode.FullReview) =>
        new(_course.Id, sectionId ?? _sectionId, title, null, null, null, passingThreshold,
            reviewMode, Questions());

    private static IReadOnlyList<QuestionBlueprint> Questions() =>
    [
        new QuestionBlueprint(
            "Capital of France",
            QuestionType.SingleChoice,
            [
                new QuestionOptionBlueprint("Paris", true),
                new QuestionOptionBlueprint("Berlin", false),
            ],
            null),
    ];

    private void StubCourse(Course? course) =>
        _courseRepository
            .FirstOrDefaultAsync(Arg.Any<ISpecification<Course>>(), Arg.Any<CancellationToken>())
            .Returns(course);
}
