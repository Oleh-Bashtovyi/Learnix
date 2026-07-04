using FluentResults;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Storage;
using Learnix.Application.Common.Constants;
using Learnix.Application.Common.Errors;
using Learnix.Application.Users.Abstractions;
using Learnix.Application.Users.Constants;
using Learnix.Application.Users.Specifications;
using MediatR;

namespace Learnix.Application.Users.Queries.GetMyProfile;

internal sealed class GetMyProfileQueryHandler(
    IUserRepository userRepository,
    IBlobStorageService blobStorage,
    ICurrentUserService currentUser)
    : IRequestHandler<GetMyProfileQuery, Result<MyProfileResponse>>
{
    public async Task<Result<MyProfileResponse>> Handle(GetMyProfileQuery request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Result.Fail<MyProfileResponse>(new AuthenticationError(CommonMessages.NotAuthenticated));

        var user = await userRepository.FirstOrDefaultAsync(
            new UserByIdSpecification(currentUser.UserId.Value),
            cancellationToken);

        if (user is null)
            return Result.Fail<MyProfileResponse>(new NotFoundError(UserMessages.GenericUserNotFound));

        return Result.Ok(new MyProfileResponse(
            user.Id,
            user.Email!,
            user.FirstName,
            user.LastName,
            user.Bio,
            !string.IsNullOrWhiteSpace(user.AvatarBlobPath) ? blobStorage.GetPublicUrl(user.AvatarBlobPath) : null,
            user.CreatedAt,
            user.PasswordHash != null));
    }
}
