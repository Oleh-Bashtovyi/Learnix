using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Enrollments.Abstractions;
using Learnix.Application.InstructorAnalytics.Queries.GetInstructorAnalyticsDynamics;
using Learnix.Application.Payments.Abstractions;

namespace Learnix.Application.UnitTests.InstructorAnalytics.Queries.GetInstructorAnalyticsDynamics;

public class GetInstructorAnalyticsDynamicsQueryHandlerTests
{
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly IEnrollmentRepository _enrollmentRepository = Substitute.For<IEnrollmentRepository>();
    private readonly IPaymentRepository _paymentRepository = Substitute.For<IPaymentRepository>();
    private readonly GetInstructorAnalyticsDynamicsQueryHandler _sut;

    private static readonly Guid InstructorId = Guid.NewGuid();

    public GetInstructorAnalyticsDynamicsQueryHandlerTests()
    {
        _currentUser.UserId.Returns(InstructorId);
        _sut = new GetInstructorAnalyticsDynamicsQueryHandler(_currentUser, _enrollmentRepository, _paymentRepository);
    }

    [Fact]
    public async Task Days_with_no_activity_are_filled_with_zero_not_skipped()
    {
        var start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var middle = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2026, 1, 3, 0, 0, 0, DateTimeKind.Utc);

        _enrollmentRepository
            .GetDailyEnrollmentCountsAsync(InstructorId, Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<DateTime, int> { [middle] = 4 });
        _paymentRepository
            .GetDailyEarningsAsync(InstructorId, Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<DateTime, decimal> { [middle] = 80m });

        var result = await _sut.Handle(new GetInstructorAnalyticsDynamicsQuery(start, end), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3);
        result.Value[0].Enrollments.Should().Be(0);
        result.Value[1].Enrollments.Should().Be(4);
        result.Value[1].Earnings.Should().Be(80m);
        result.Value[2].Enrollments.Should().Be(0);
    }

    [Fact]
    public async Task Counting_and_summing_happen_in_the_database_not_by_loading_every_row()
    {
        _enrollmentRepository
            .GetDailyEnrollmentCountsAsync(InstructorId, Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<DateTime, int>());
        _paymentRepository
            .GetDailyEarningsAsync(InstructorId, Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<DateTime, decimal>());

        await _sut.Handle(
            new GetInstructorAnalyticsDynamicsQuery(DateTime.UtcNow.AddDays(-7), DateTime.UtcNow),
            CancellationToken.None);

        await _enrollmentRepository.DidNotReceiveWithAnyArgs().ListAsync(default!, default);
        await _paymentRepository.DidNotReceiveWithAnyArgs().ListAsync(default!, default);
    }
}
