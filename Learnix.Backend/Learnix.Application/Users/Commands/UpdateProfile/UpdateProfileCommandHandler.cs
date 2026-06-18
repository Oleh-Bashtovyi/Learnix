using Learnix.Application.Common.Constants;
using Learnix.Application.Users.Constants;
using FluentResults;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Abstractions.Storage;
using Learnix.Application.Common.Errors;
using Learnix.Application.Users.Abstractions;
using Learnix.Application.Users.Specifications;
using MediatR;

namespace Learnix.Application.Users.Commands.UpdateProfile;

internal sealed class UpdateProfileCommandHandler(
    IUserRepository userRepository,
    IBlobStorageService blobStorage,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser)
    : IRequestHandler<UpdateProfileCommand, Result>
{
    public async Task<Result> Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Result.Fail(new AuthenticationError(CommonMessages.NotAuthenticated));

        var user = await userRepository.FirstOrDefaultAsync(
            new UserByIdSpecification(currentUser.UserId.Value, forUpdate: true),
            cancellationToken);

        if (user is null)
            return Result.Fail(new NotFoundError(UserMessages.GenericUserNotFound));

        user.UpdateProfile(request.FirstName, request.LastName, request.Bio);

        if (request.AvatarBlobPath is not null && request.AvatarBlobPath != user.AvatarBlobPath)
        {
            var validateResult = await blobStorage.ValidateAsync(
                request.AvatarBlobPath, UploadTarget.Avatar, cancellationToken);

            if (validateResult.IsFailed)
                return Result.Fail(validateResult.Errors);

            user.SetAvatar(request.AvatarBlobPath);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
