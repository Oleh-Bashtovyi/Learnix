using FluentValidation;
using Learnix.Domain.Constants;

namespace Learnix.Application.InstructorApplications.Commands.RejectApplication;

public sealed class RejectApplicationValidator : AbstractValidator<RejectApplicationCommand>
{
    public RejectApplicationValidator()
    {
        RuleFor(x => x.RejectionReason)
            .MaximumLength(InstructorApplicationConstants.RejectionReasonMaxLength)
            .WithMessage("Rejection reason must not exceed 1000 characters.")
            .When(x => x.RejectionReason is not null);
    }
}
