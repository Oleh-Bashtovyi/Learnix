using Learnix.Domain.Common;
using Learnix.Domain.Common.Exceptions;

namespace Learnix.Domain.Entities;

public class CourseReview : BaseEntity
{
    private CourseReview() { }

    private CourseReview(Guid courseId, Guid studentId, int rating, string? comment)
    {
        CourseId = courseId;
        StudentId = studentId;
        SetRating(rating);
        Comment = comment;
    }

    public Guid CourseId { get; private set; }
    public Guid StudentId { get; private set; }
    // S1144: no code calls the setter — EF Core materializes the navigation.
#pragma warning disable S1144
    public User? Student { get; private set; }
#pragma warning restore S1144

    public int Rating { get; private set; }
    public string? Comment { get; private set; }

    /// <summary>
    /// How many lessons the student had completed — and how many the course had — at the moment the
    /// review was written or last edited. A credibility signal for weighting reviews: a rating left
    /// after finishing the course carries more weight than one left after a single lesson. Zero on
    /// legacy rows written before this was captured.
    /// </summary>
    public int CompletedLessonsAtReview { get; private set; }
    public int TotalLessonsAtReview { get; private set; }

    public static CourseReview Create(Guid courseId, Guid studentId, int rating, string? comment = null)
        => new(courseId, studentId, rating, comment);

    public void Update(int rating, string? comment)
    {
        SetRating(rating);
        Comment = comment;
    }

    /// <summary>Records the student's course progress at review time (see the properties it writes).</summary>
    public void CaptureProgress(int completedLessons, int totalLessons)
    {
        CompletedLessonsAtReview = completedLessons;
        TotalLessonsAtReview = totalLessons;
    }

    private void SetRating(int rating)
    {
        if (rating is < 1 or > 5)
            throw new DomainException("Course review rating must be between 1 and 5.");

        Rating = rating;
    }
}
