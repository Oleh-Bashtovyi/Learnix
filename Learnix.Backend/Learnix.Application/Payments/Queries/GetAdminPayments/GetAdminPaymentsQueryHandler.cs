using FluentResults;
using Learnix.Application.Common.Pagination;
using Learnix.Application.Payments.Abstractions;
using Learnix.Application.Payments.Specifications;
using MediatR;

namespace Learnix.Application.Payments.Queries.GetAdminPayments;

public sealed class GetAdminPaymentsQueryHandler(
    IPaymentRepository paymentRepository)
    : IRequestHandler<GetAdminPaymentsQuery, Result<PaginatedResult<AdminPaymentDto>>>
{
    public async Task<Result<PaginatedResult<AdminPaymentDto>>> Handle(
        GetAdminPaymentsQuery request,
        CancellationToken cancellationToken)
    {
        var pagination = PaginationRequest.FromOffset(request.Skip, request.Take);

        var totalCount = await paymentRepository.CountAsync(
            new AllPaymentsCountSpecification(request.Search),
            cancellationToken);

        if (totalCount == 0)
            return Result.Ok(PaginatedResult<AdminPaymentDto>.Empty(pagination.PageIndex, pagination.PageSize));

        var payments = await paymentRepository.ListAsync(
            new AllPaymentsSpecification(request.Search, pagination.Skip, pagination.Take),
            cancellationToken);

        var items = payments.Select(p => new AdminPaymentDto(
            p.Id,
            p.UserId,
            p.User?.Email ?? string.Empty,
            p.CourseId,
            p.Course?.Title ?? string.Empty,
            p.Amount,
            p.Currency,
            p.Status.ToString(),
            p.PaymentProvider,
            p.CreatedAt,
            p.CompletedAt));

        return Result.Ok(PaginatedResult<AdminPaymentDto>.Create(
            items,
            pagination.PageIndex,
            pagination.PageSize,
            totalCount));
    }
}
