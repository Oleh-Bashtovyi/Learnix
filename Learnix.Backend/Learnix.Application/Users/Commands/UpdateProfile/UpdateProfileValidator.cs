using FluentValidation;
using Learnix.Domain.Constants;

namespace Learnix.Application.Users.Commands.UpdateProfile;

public sealed class UpdateProfileValidator : AbstractValidator<UpdateProfileCommand>
{
    public UpdateProfileValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty()
            .MaximumLength(UserConstants.FirstNameMaxLength);

        RuleFor(x => x.LastName)
            .NotEmpty()
            .MaximumLength(UserConstants.LastNameMaxLength);

        RuleFor(x => x.Bio)
            .MaximumLength(UserConstants.BioMaxLength)
            .When(x => x.Bio is not null);

        RuleFor(x => x.AvatarBlobPath)
            .MaximumLength(UserConstants.AvatarUrlMaxLength)
            .When(x => x.AvatarBlobPath is not null);
    }
}
