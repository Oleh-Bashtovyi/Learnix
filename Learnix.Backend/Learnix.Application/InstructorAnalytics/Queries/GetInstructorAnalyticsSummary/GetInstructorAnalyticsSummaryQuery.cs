using FluentResults;
using MediatR;

namespace Learnix.Application.InstructorAnalytics.Queries.GetInstructorAnalyticsSummary;

public sealed record GetInstructorAnalyticsSummaryQuery : IRequest<Result<InstructorAnalyticsSummaryDto>>;

public sealed record InstructorAnalyticsSummaryDto(
    int TotalStudents,
    decimal TotalRevenue,
    double AverageRating,
    int CertificatesIssued);
