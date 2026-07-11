using Learnix.Domain.Common.Exceptions;
using Learnix.Domain.Entities;

namespace Learnix.Domain.UnitTests.Entities;

public class CourseReviewTests
{
    private static CourseReview Valid()
        => CourseReview.Create(Guid.NewGuid(), Guid.NewGuid(), rating: 4, comment: "Solid course");

    [Fact]
    public void Create_ShouldSetRatingAndComment()
    {
        // Arrange
        var courseId = Guid.NewGuid();
        var studentId = Guid.NewGuid();

        // Act
        var review = CourseReview.Create(courseId, studentId, rating: 5, comment: "Great");

        // Assert
        review.CourseId.Should().Be(courseId);
        review.StudentId.Should().Be(studentId);
        review.Rating.Should().Be(5);
        review.Comment.Should().Be("Great");
    }

    [Fact]
    public void Create_ShouldAllowAReviewWithoutAComment()
    {
        // Act — a rating alone is a valid review
        var review = CourseReview.Create(Guid.NewGuid(), Guid.NewGuid(), rating: 3);

        // Assert
        review.Comment.Should().BeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(6)]
    public void Create_WhenRatingIsOutsideOneToFive_ShouldThrowDomainException(int rating)
    {
        // Act
        var act = () => CourseReview.Create(Guid.NewGuid(), Guid.NewGuid(), rating);

        // Assert — the course's average rating is computed from these, so the bound is load-bearing
        act.Should().Throw<DomainException>()
            .WithMessage("Course review rating must be between 1 and 5.");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    public void Create_ShouldAcceptTheBoundaryRatings(int rating)
    {
        // Act
        var review = CourseReview.Create(Guid.NewGuid(), Guid.NewGuid(), rating);

        // Assert
        review.Rating.Should().Be(rating);
    }

    [Fact]
    public void Update_ShouldReplaceRatingAndComment()
    {
        // Arrange
        var review = Valid();

        // Act
        review.Update(rating: 2, comment: "Changed my mind");

        // Assert
        review.Rating.Should().Be(2);
        review.Comment.Should().Be("Changed my mind");
    }

    [Fact]
    public void Update_WhenRatingIsInvalid_ShouldThrowAndLeaveTheReviewUnchanged()
    {
        // Arrange
        var review = Valid();

        // Act
        var act = () => review.Update(rating: 9, comment: "Should not stick");

        // Assert — the rating is validated before anything is assigned
        act.Should().Throw<DomainException>();
        review.Rating.Should().Be(4);
        review.Comment.Should().Be("Solid course");
    }
}
