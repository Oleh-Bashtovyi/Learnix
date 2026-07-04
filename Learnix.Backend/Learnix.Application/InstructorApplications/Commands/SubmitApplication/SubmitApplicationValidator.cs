using FluentValidation;
using Learnix.Domain.Constants;

namespace Learnix.Application.InstructorApplications.Commands.SubmitApplication;

public sealed class SubmitApplicationValidator : AbstractValidator<SubmitApplicationCommand>
{
    public SubmitApplicationValidator()
    {
        RuleFor(x => x.MotivationText)
            .NotEmpty()
            .WithMessage("Motivation text is required.")
            .MaximumLength(InstructorApplicationConstants.MotivationTextMaxLength)
            .WithMessage($"Motivation text must not exceed {InstructorApplicationConstants.MotivationTextMaxLength} characters.");

        RuleFor(x => x.PortfolioUrl)
            .MaximumLength(InstructorApplicationConstants.PortfolioUrlMaxLength)
            .WithMessage($"Portfolio URL must not exceed {InstructorApplicationConstants.PortfolioUrlMaxLength} characters.")
            .Must(url => url is null || Uri.TryCreate(url, UriKind.Absolute, out _))
            .WithMessage("Portfolio URL must be a valid URL.")
            .When(x => x.PortfolioUrl is not null);
    }
}
