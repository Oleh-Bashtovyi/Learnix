using FluentResults;
using Learnix.Application.Common.Pagination;
using MediatR;

namespace Learnix.Application.Payments.Queries.GetAdminPayments;

public sealed record GetAdminPaymentsQuery(string? Search, int Skip, int Take)
    : IRequest<Result<PaginatedResult<AdminPaymentDto>>>;
