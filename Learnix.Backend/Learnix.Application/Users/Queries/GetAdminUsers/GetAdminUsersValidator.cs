using FluentValidation;
using Learnix.Application.Common.Pagination;

namespace Learnix.Application.Users.Queries.GetAdminUsers;

internal sealed class GetAdminUsersValidator : AbstractValidator<GetAdminUsersQuery>
{
    public GetAdminUsersValidator()
    {
        RuleFor(x => x.Skip).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Take)
            .InclusiveBetween(1, PaginationRequest.MaxPageSize);
    }
}
