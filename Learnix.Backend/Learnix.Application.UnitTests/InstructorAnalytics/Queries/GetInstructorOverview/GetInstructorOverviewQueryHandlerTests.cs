using Ardalis.Specification;
using Learnix.Application.Certificates.Abstractions;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.Enrollments.Abstractions;
using Learnix.Application.InstructorAnalytics.Queries.GetInstructorOverview;
using Learnix.Application.Payments.Abstractions;
using Learnix.Application.Reviews.Abstractions;
using Learnix.Domain.Entities;

namespace Learnix.Application.UnitTests.InstructorAnalytics.Queries.GetInstructorOverview;

public class GetInstructorOverviewQueryHandlerTests
{
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly ICourseRepository _courseRepository = Substitute.For<ICourseRepository>();
    private readonly IEnrollmentRepository _enrollmentRepository = Substitute.For<IEnrollmentRepository>();
    private readonly IPaymentRepository _paymentRepository = Substitute.For<IPaymentRepository>();
    private readonly ICertificateRepository _certificateRepository = Substitute.For<ICertificateRepository>();
    private readonly ICourseReviewRepository _reviewRepository = Substitute.For<ICourseReviewRepository>();
    private readonly GetInstructorOverviewQueryHandler _sut;

    private static readonly Guid InstructorId = Guid.NewGuid();

    public GetInstructorOverviewQueryHandlerTests()
    {
        _currentUser.UserId.Returns(InstructorId);
        _sut = new GetInstructorOverviewQueryHandler(
            _currentUser, _courseRepository, _enrollmentRepository, _paymentRepository,
            _certificateRepository, _reviewRepository);

        _courseRepository
            .ListAsync(Arg.Any<ISpecification<Course>>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _paymentRepository
            .GetTotalEarningsAsync(InstructorId, Arg.Any<CancellationToken>())
            .Returns(120m);
        _reviewRepository
            .GetRatingDistributionAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<int, int>());
    }

    [Fact]
    public async Task Revenue_is_read_from_the_database_sum_not_loaded_and_summed_in_memory()
    {
        var result = await _sut.Handle(new GetInstructorOverviewQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Summary.TotalRevenue.Should().Be(120m);
        await _paymentRepository.DidNotReceiveWithAnyArgs()
            .ListAsync(Arg.Any<ISpecification<Payment>>(), default);
    }
}
