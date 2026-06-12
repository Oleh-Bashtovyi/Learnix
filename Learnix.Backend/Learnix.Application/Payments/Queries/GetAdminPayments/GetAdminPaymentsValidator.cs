using FluentValidation;
using Learnix.Application.Common.Pagination;

namespace Learnix.Application.Payments.Queries.GetAdminPayments;

internal sealed class GetAdminPaymentsValidator : AbstractValidator<GetAdminPaymentsQuery>
{
    public GetAdminPaymentsValidator()
    {
        RuleFor(x => x.Skip).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Take)
            .InclusiveBetween(1, PaginationRequest.MaxPageSize);
    }
}
