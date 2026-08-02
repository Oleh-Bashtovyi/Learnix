using FluentResults;
using MediatR;

namespace Learnix.Application.InstructorAnalytics.Queries.GetInstructorAnalyticsSummary;

public sealed record GetInstructorAnalyticsSummaryQuery : IRequest<Result<InstructorAnalyticsSummaryDto>>;

/// <param name="TotalStudents">Distinct students across the instructor's courses, all time.</param>
/// <param name="TotalRevenue">Completed-payment revenue, all time.</param>
/// <param name="CertificatesIssued">Certificates issued, all time.</param>
/// <param name="NewStudentsTrend">Students who arrived in the trend window.</param>
/// <param name="RevenueTrend">Revenue earned in the trend window.</param>
/// <param name="CertificatesTrend">Certificates issued in the trend window.</param>
/// <remarks>
/// The totals are all-time; the trends measure only the window. They answer different questions and
/// are deliberately separate fields — a percentage attached to an all-time total would describe
/// neither.
/// </remarks>
public sealed record InstructorAnalyticsSummaryDto(
    int TotalStudents,
    decimal TotalRevenue,
    double AverageRating,
    int CertificatesIssued,
    InstructorAnalyticsTrendDto NewStudentsTrend,
    InstructorAnalyticsTrendDto RevenueTrend,
    InstructorAnalyticsTrendDto CertificatesTrend);

/// <param name="Current">What the window added.</param>
/// <param name="ChangePercent">
/// Change against the window of equal length before it. Null when that earlier window is empty:
/// there is no percentage change from nothing, and reporting one would invent a baseline.
/// </param>
public sealed record InstructorAnalyticsTrendDto(decimal Current, double? ChangePercent);
