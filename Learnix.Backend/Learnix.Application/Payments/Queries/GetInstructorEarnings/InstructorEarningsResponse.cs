namespace Learnix.Application.Payments.Queries.GetInstructorEarnings;

public sealed record InstructorEarningsResponse(
    decimal TotalEarnings,
    int TotalPayments,
    IReadOnlyList<CourseEarningsDto> Courses);
