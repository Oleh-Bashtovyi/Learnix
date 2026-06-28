using FluentResults;
using Learnix.Application.Common.Abstractions.Storage;
using Learnix.Application.Common.Errors;
using Learnix.Application.Users.Abstractions;
using Learnix.Application.Users.Constants;
using Learnix.Application.Users.Specifications;
using MediatR;

namespace Learnix.Application.Users.Queries.GetUserProfile;

internal sealed class GetUserProfileQueryHandler(
    IUserRepository userRepository,
    IBlobStorageService blobStorage)
    : IRequestHandler<GetUserProfileQuery, Result<UserProfileResponse>>
{
    public async Task<Result<UserProfileResponse>> Handle(GetUserProfileQuery request, CancellationToken cancellationToken)
    {
        var user = await userRepository.FirstOrDefaultAsync(
            new UserByIdSpecification(request.UserId),
            cancellationToken);

        if (user is null)
            return Result.Fail<UserProfileResponse>(new NotFoundError(UserMessages.GenericUserNotFound));

        return Result.Ok(new UserProfileResponse(
            user.Id,
            user.FirstName,
            user.LastName,
            user.Bio,
            !string.IsNullOrWhiteSpace(user.AvatarBlobPath) ? blobStorage.GetPublicUrl(user.AvatarBlobPath) : null));
    }
}
