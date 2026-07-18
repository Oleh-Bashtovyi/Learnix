using FluentResults;
using MediatR;

namespace Learnix.Application.InstructorAnalytics.Queries.GetInstructorEngagement;

public sealed record GetInstructorEngagementQuery : IRequest<Result<InstructorEngagementDto>>;

/// <summary>
/// The learning funnel across the instructor's courses (each stage counted at student×course
/// granularity) plus how many students were active in the last 30 days.
/// </summary>
public sealed record InstructorEngagementDto(
    int Enrolled,
    int Started,
    int Completed,
    int Certified,
    int ActiveStudentsLast30Days);
