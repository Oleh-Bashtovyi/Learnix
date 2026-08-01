using Ardalis.Specification;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Errors;
using Learnix.Application.Payments.Abstractions;
using Learnix.Application.Payments.Queries.GetMyEarnings;
using Learnix.Domain.Entities;

namespace Learnix.Application.UnitTests.Payments.Queries.GetMyEarnings;

public class GetMyEarningsQueryHandlerTests
{
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly IPaymentRepository _paymentRepository = Substitute.For<IPaymentRepository>();
    private readonly GetMyEarningsQueryHandler _sut;

    private static readonly Guid InstructorId = Guid.NewGuid();

    public GetMyEarningsQueryHandlerTests()
    {
        _currentUser.UserId.Returns(InstructorId);
        _sut = new GetMyEarningsQueryHandler(_currentUser, _paymentRepository);
    }

    private void HasPayments(params Payment[] payments) =>
        _paymentRepository
            .ListAsync(Arg.Any<ISpecification<Payment>>(), Arg.Any<CancellationToken>())
            .Returns(payments.ToList());

    [Fact]
    public async Task An_anonymous_caller_gets_an_authentication_error()
    {
        _currentUser.UserId.Returns((Guid?)null);

        var result = await _sut.Handle(new GetMyEarningsQuery(), CancellationToken.None);

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().BeOfType<AuthenticationError>();
    }

    [Fact]
    public async Task No_payments_is_a_zeroed_result_not_an_error()
    {
        HasPayments();

        var result = await _sut.Handle(new GetMyEarningsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalEarnings.Should().Be(0m);
        result.Value.TotalPayments.Should().Be(0);
        result.Value.Courses.Should().BeEmpty();
    }

    [Fact]
    public async Task Earnings_are_grouped_and_summed_per_course()
    {
        var courseA = Guid.NewGuid();
        var courseB = Guid.NewGuid();
        HasPayments(
            Payment.CreateMock(Guid.NewGuid(), courseA, Guid.NewGuid(), 20m),
            Payment.CreateMock(Guid.NewGuid(), courseA, Guid.NewGuid(), 20m),
            Payment.CreateMock(Guid.NewGuid(), courseB, Guid.NewGuid(), 50m));

        var result = await _sut.Handle(new GetMyEarningsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalEarnings.Should().Be(90m);
        result.Value.TotalPayments.Should().Be(3);
        result.Value.Courses.Should().HaveCount(2);
        result.Value.Courses.Should().ContainSingle(c => c.CourseId == courseA && c.PaymentsCount == 2 && c.TotalAmount == 40m);
        result.Value.Courses.Should().ContainSingle(c => c.CourseId == courseB && c.PaymentsCount == 1 && c.TotalAmount == 50m);
    }
}
