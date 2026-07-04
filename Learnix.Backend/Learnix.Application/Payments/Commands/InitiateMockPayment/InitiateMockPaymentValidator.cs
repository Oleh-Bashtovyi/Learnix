using FluentValidation;

namespace Learnix.Application.Payments.Commands.InitiateMockPayment;

public sealed class InitiateMockPaymentValidator : AbstractValidator<InitiateMockPaymentCommand>
{
    public InitiateMockPaymentValidator()
    {
        RuleFor(x => x.CourseId).NotEmpty();
    }
}
