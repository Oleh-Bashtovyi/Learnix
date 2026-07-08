using Learnix.Domain.Common.Exceptions;
using Learnix.Domain.Entities;
using Learnix.Domain.Enums;
using Learnix.Domain.Events.Course;

namespace Learnix.Domain.UnitTests.Entities;

public class CourseTests
{
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

    [Fact]
    public void Publish_WhenNoCoverImage_ShouldThrowDomainException()
    {
        // Arrange
        var course = Course.Create(Guid.NewGuid(), Guid.NewGuid(), "Title", "Desc", 100);
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
        var course = Course.Create(Guid.NewGuid(), Guid.NewGuid(), "Title", "Desc", 100);
        course.SetCoverImage("cover.jpg");

        // Act
        var act = () => course.Publish();

        // Assert
        act.Should().Throw<DomainException>()
           .WithMessage("Published course must have at least one section.");
    }
}
