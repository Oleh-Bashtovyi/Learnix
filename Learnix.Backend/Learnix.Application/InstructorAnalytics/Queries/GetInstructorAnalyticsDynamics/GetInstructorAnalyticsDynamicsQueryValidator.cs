using FluentValidation;
using Learnix.Application.InstructorAnalytics.Constants;

namespace Learnix.Application.InstructorAnalytics.Queries.GetInstructorAnalyticsDynamics;

public sealed class GetInstructorAnalyticsDynamicsQueryValidator : AbstractValidator<GetInstructorAnalyticsDynamicsQuery>
{
    public GetInstructorAnalyticsDynamicsQueryValidator()
    {
        // NotEmpty rejects default(DateTime) (0001-01-01) — the value bound when the query param is missing,
        // which would otherwise send the per-day loop through ~740k iterations.
        RuleFor(x => x.StartDate).NotEmpty();
        RuleFor(x => x.EndDate).NotEmpty();

        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.StartDate)
            .WithMessage("End date must be on or after the start date.");

        RuleFor(x => x)
            .Must(x => (x.EndDate.Date - x.StartDate.Date).TotalDays <= InstructorAnalyticsConstants.MaxDynamicsRangeDays)
            .WithMessage($"The date range cannot exceed {InstructorAnalyticsConstants.MaxDynamicsRangeDays} days.");
    }
}
