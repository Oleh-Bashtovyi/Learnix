using Ardalis.Specification;
using Learnix.Application.Common.Errors;
using Learnix.Application.Payments.Abstractions;
using Learnix.Application.Payments.Queries.GetInstructorEarnings;
using Learnix.Application.Users.Abstractions;
using Learnix.Domain.Entities;

namespace Learnix.Application.UnitTests.Payments.Queries.GetInstructorEarnings;

public class GetInstructorEarningsQueryHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IPaymentRepository _paymentRepository = Substitute.For<IPaymentRepository>();
    private readonly GetInstructorEarningsQueryHandler _sut;

    private static readonly Guid InstructorId = Guid.NewGuid();

    public GetInstructorEarningsQueryHandlerTests()
    {
        _sut = new GetInstructorEarningsQueryHandler(_userRepository, _paymentRepository);

        _userRepository
            .FirstOrDefaultAsync(Arg.Any<ISingleResultSpecification<User>>(), Arg.Any<CancellationToken>())
            .Returns(new User("instructor@learnix.dev", "Ada", "Lovelace"));
    }

    private void HasPayments(params Payment[] payments) =>
        _paymentRepository
            .ListAsync(Arg.Any<ISpecification<Payment>>(), Arg.Any<CancellationToken>())
            .Returns(payments.ToList());

    [Fact]
    public async Task A_nonexistent_instructor_id_is_not_found_not_a_zeroed_result()
    {
        _userRepository
            .FirstOrDefaultAsync(Arg.Any<ISingleResultSpecification<User>>(), Arg.Any<CancellationToken>())
            .Returns((User?)null);

        var result = await _sut.Handle(new GetInstructorEarningsQuery(InstructorId), CancellationToken.None);

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().BeOfType<NotFoundError>();
        await _paymentRepository.DidNotReceiveWithAnyArgs()
            .ListAsync(Arg.Any<ISpecification<Payment>>(), default);
    }

    [Fact]
    public async Task An_existing_user_with_no_payments_is_a_zeroed_result_not_an_error()
    {
        HasPayments();

        var result = await _sut.Handle(new GetInstructorEarningsQuery(InstructorId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalEarnings.Should().Be(0m);
        result.Value.Courses.Should().BeEmpty();
    }

    [Fact]
    public async Task Earnings_are_computed_for_the_requested_instructor_not_the_caller()
    {
        var courseId = Guid.NewGuid();
        HasPayments(Payment.CreateMock(Guid.NewGuid(), courseId, Guid.NewGuid(), 75m));

        var result = await _sut.Handle(new GetInstructorEarningsQuery(InstructorId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalEarnings.Should().Be(75m);
        result.Value.Courses.Should().ContainSingle(c => c.CourseId == courseId);
    }
}
