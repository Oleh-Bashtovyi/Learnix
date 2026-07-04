namespace Learnix.Application.Enrollments.Queries.GetMyEnrollments;

public sealed record EnrolledCourseDto(
    Guid EnrollmentId,
    Guid CourseId,
    string CourseTitle,
    string? CourseCoverBlobPath,
    Guid CourseInstructorId,
    Guid CourseCategoryId,
    decimal PricePaid,
    string EnrollmentStatus,
    string PaymentStatus,
    DateTime EnrolledAt,
    DateTime? CompletedAt,
    string? CoverImageUrl);
