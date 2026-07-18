using FluentResults;
using MediatR;

namespace Learnix.Application.InstructorAnalytics.Queries.GetInstructorRatingDistribution;

public sealed record GetInstructorRatingDistributionQuery(Guid? CourseId = null)
    : IRequest<Result<InstructorRatingDistributionDto>>;

public sealed record InstructorRatingDistributionDto(
    int OneStar,
    int TwoStar,
    int ThreeStar,
    int FourStar,
    int FiveStar);
