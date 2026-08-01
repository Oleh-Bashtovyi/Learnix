using FluentValidation;

namespace Learnix.Application.Payments.Queries.GetInstructorEarnings;

public sealed class GetInstructorEarningsQueryValidator : AbstractValidator<GetInstructorEarningsQuery>
{
    public GetInstructorEarningsQueryValidator()
    {
        RuleFor(x => x.InstructorId).NotEmpty();
    }
}
