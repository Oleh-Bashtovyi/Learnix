using FluentResults;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Storage;
using Learnix.Application.Common.Constants;
using Learnix.Application.Common.Errors;
using Learnix.Application.Common.Pagination;
using Learnix.Application.Users.Abstractions;
using Learnix.Application.Users.Constants;
using Learnix.Application.Users.Specifications;
using Learnix.Domain.Constants;
using MediatR;

namespace Learnix.Application.Users.Queries.GetAdminUsers;

internal sealed class GetAdminUsersQueryHandler(
    ICurrentUserService currentUser,
    IUserRepository userRepository,
    IUserRoleService roleService,
    IBlobStorageService blobStorage)
    : IRequestHandler<GetAdminUsersQuery, Result<PaginatedResult<AdminUserDto>>>
{
    public async Task<Result<PaginatedResult<AdminUserDto>>> Handle(
        GetAdminUsersQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Result.Fail(new AuthenticationError(CommonMessages.NotAuthenticated));

        if (!currentUser.IsInRole(Roles.Admin))
            return Result.Fail(new ForbiddenError(UserMessages.OnlyAdminsCanListUsers));

        var pagination = PaginationRequest.FromOffset(request.Skip, request.Take);

        var totalCount = await userRepository.CountAsync(
            new AdminUserListCountSpecification(request.Search, request.IncludeDeleted),
            cancellationToken);

        if (totalCount == 0)
            return Result.Ok(PaginatedResult<AdminUserDto>.Empty(pagination.PageIndex, pagination.PageSize));

        var users = await userRepository.ListAsync(
            new AdminUserListSpecification(request.Search, pagination.Skip, pagination.Take, request.IncludeDeleted),
            cancellationToken);

        var roleMap = await roleService.GetRolesBulkAsync(
            users.Select(u => u.Id), cancellationToken);

        var dtos = users.Select(user => new AdminUserDto(
            user.Id,
            user.Email!,
            user.FirstName,
            user.LastName,
            !string.IsNullOrWhiteSpace(user.AvatarBlobPath) ? blobStorage.GetPublicUrl(user.AvatarBlobPath) : null,
            roleMap.TryGetValue(user.Id, out var roles) ? roles : [],
            user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow,
            user.IsDeleted,
            user.CreatedAt)).ToList();

        return Result.Ok(PaginatedResult<AdminUserDto>.Create(dtos, pagination.PageIndex, pagination.PageSize, totalCount));
    }
}
