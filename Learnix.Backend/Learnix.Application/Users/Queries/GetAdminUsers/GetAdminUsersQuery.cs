using FluentResults;
using Learnix.Application.Common.Pagination;
using MediatR;

namespace Learnix.Application.Users.Queries.GetAdminUsers;

public sealed record GetAdminUsersQuery(
    string? Search,
    int Skip,
    int Take,
    bool IncludeDeleted
) : IRequest<Result<PaginatedResult<AdminUserDto>>>;
