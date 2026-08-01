namespace Learnix.Application.Payments.Models;

public sealed record InstructorEarningsResponse(
    decimal TotalEarnings,
    int TotalPayments,
    IReadOnlyList<CourseEarningsDto> Courses);
