using FluentResults;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Constants;
using Learnix.Application.Common.Errors;
using Learnix.Application.Common.Pagination;
using Learnix.Application.Payments.Abstractions;
using Learnix.Application.Payments.Specifications;
using MediatR;

namespace Learnix.Application.Payments.Queries.GetMyPayments;

public sealed class GetMyPaymentsQueryHandler(
    ICurrentUserService currentUser,
    IPaymentRepository paymentRepository)
    : IRequestHandler<GetMyPaymentsQuery, Result<PaginatedResult<PaymentDto>>>
{
    public async Task<Result<PaginatedResult<PaymentDto>>> Handle(
        GetMyPaymentsQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Result.Fail(new AuthenticationError(CommonMessages.NotAuthenticated));

        var userId = currentUser.UserId.Value;
        var pagination = PaginationRequest.FromOffset(request.Skip, request.Take);

        var totalCount = await paymentRepository.CountAsync(
            new MyPaymentsCountSpecification(userId),
            cancellationToken);

        if (totalCount == 0)
            return Result.Ok(PaginatedResult<PaymentDto>.Empty(pagination.PageIndex, pagination.PageSize));

        var payments = await paymentRepository.ListAsync(
            new MyPaymentsSpecification(userId, pagination.Skip, pagination.Take),
            cancellationToken);

        var items = payments.Select(p => new PaymentDto(
            p.Id,
            p.CourseId,
            p.Course?.Title ?? string.Empty,
            p.Amount,
            p.Currency,
            p.Status.ToString(),
            p.PaymentProvider,
            p.CreatedAt,
            p.CompletedAt));

        return Result.Ok(PaginatedResult<PaymentDto>.Create(
            items,
            pagination.PageIndex,
            pagination.PageSize,
            totalCount));
    }
}
