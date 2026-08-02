namespace Learnix.Application.Enrollments.Queries.GetMyEnrollments;

public sealed record EnrolledCourseDto(
    Guid EnrollmentId,
    Guid CourseId,
    string CourseTitle,
    string? CourseCoverBlobPath,
    Guid CourseInstructorId,
    string InstructorName,
    Guid CourseCategoryId,
    decimal PricePaid,
    string EnrollmentStatus,
    string PaymentStatus,
    DateTime EnrolledAt,
    DateTime? CompletedAt,
    string? CoverImageUrl,
    int CompletedLessons,
    int TotalLessons,
    int? MyRating);
