using FluentResults;
using Learnix.Application.Common.Pagination;
using MediatR;

namespace Learnix.Application.Payments.Queries.GetMyPayments;

public sealed record GetMyPaymentsQuery(int Skip, int Take)
    : IRequest<Result<PaginatedResult<PaymentDto>>>;
