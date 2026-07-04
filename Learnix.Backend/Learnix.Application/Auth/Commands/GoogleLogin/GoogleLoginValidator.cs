using FluentValidation;

namespace Learnix.Application.Auth.Commands.GoogleLogin;

public sealed class GoogleLoginValidator : AbstractValidator<GoogleLoginCommand>
{
    public GoogleLoginValidator()
    {
        RuleFor(x => x.IdToken).NotEmpty();
    }
}
