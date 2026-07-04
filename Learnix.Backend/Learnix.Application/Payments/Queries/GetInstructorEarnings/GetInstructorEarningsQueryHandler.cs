using FluentResults;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Constants;
using Learnix.Application.Common.Errors;
using Learnix.Application.Payments.Abstractions;
using Learnix.Application.Payments.Specifications;
using MediatR;

namespace Learnix.Application.Payments.Queries.GetInstructorEarnings;

public sealed class GetInstructorEarningsQueryHandler(
    ICurrentUserService currentUser,
    IPaymentRepository paymentRepository)
    : IRequestHandler<GetInstructorEarningsQuery, Result<InstructorEarningsResponse>>
{
    public async Task<Result<InstructorEarningsResponse>> Handle(
        GetInstructorEarningsQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Result.Fail(new AuthenticationError(CommonMessages.NotAuthenticated));

        var instructorId = currentUser.UserId.Value;

        var payments = await paymentRepository.ListAsync(
            new InstructorPaymentsSpecification(instructorId),
            cancellationToken);

        if (payments.Count == 0)
            return Result.Ok(new InstructorEarningsResponse(0m, 0, []));

        var courses = payments
            .GroupBy(p => p.CourseId)
            .Select(g => new CourseEarningsDto(
                g.Key,
                g.First().Course?.Title ?? string.Empty,
                g.Count(),
                g.Sum(p => p.Amount),
                g.Max(p => p.CreatedAt)))
            .OrderByDescending(c => c.TotalAmount)
            .ToList();

        var totalEarnings = courses.Sum(c => c.TotalAmount);
        var totalPayments = courses.Sum(c => c.PaymentsCount);

        return Result.Ok(new InstructorEarningsResponse(totalEarnings, totalPayments, courses));
    }
}
