using Ardalis.Specification;
using Learnix.Application.Certificates.Abstractions;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.Enrollments.Abstractions;
using Learnix.Application.InstructorAnalytics.Queries.GetInstructorAnalyticsSummary;
using Learnix.Application.Payments.Abstractions;
using Learnix.Domain.Entities;

namespace Learnix.Application.UnitTests.InstructorAnalytics.Queries.GetInstructorAnalyticsSummary;

public class GetInstructorAnalyticsSummaryQueryHandlerTests
{
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly ICourseRepository _courseRepository = Substitute.For<ICourseRepository>();
    private readonly IEnrollmentRepository _enrollmentRepository = Substitute.For<IEnrollmentRepository>();
    private readonly IPaymentRepository _paymentRepository = Substitute.For<IPaymentRepository>();
    private readonly ICertificateRepository _certificateRepository = Substitute.For<ICertificateRepository>();
    private readonly GetInstructorAnalyticsSummaryQueryHandler _sut;

    private static readonly Guid InstructorId = Guid.NewGuid();

    public GetInstructorAnalyticsSummaryQueryHandlerTests()
    {
        _currentUser.UserId.Returns(InstructorId);
        _sut = new GetInstructorAnalyticsSummaryQueryHandler(
            _currentUser, _courseRepository, _enrollmentRepository, _paymentRepository, _certificateRepository);

        _courseRepository
            .ListAsync(Arg.Any<ISpecification<Course>>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _paymentRepository
            .GetTotalEarningsAsync(InstructorId, Arg.Any<CancellationToken>())
            .Returns(340m);
        _certificateRepository
            .CountAsync(Arg.Any<ISpecification<Certificate>>(), Arg.Any<CancellationToken>())
            .Returns(2);
        _enrollmentRepository
            .CountDistinctStudentsForInstructorAsync(InstructorId, Arg.Any<CancellationToken>())
            .Returns(9);
    }

    [Fact]
    public async Task Revenue_is_read_from_the_database_sum_not_loaded_and_summed_in_memory()
    {
        var result = await _sut.Handle(new GetInstructorAnalyticsSummaryQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalRevenue.Should().Be(340m);
        result.Value.TotalStudents.Should().Be(9);
        result.Value.CertificatesIssued.Should().Be(2);
        await _paymentRepository.DidNotReceiveWithAnyArgs()
            .ListAsync(Arg.Any<ISpecification<Payment>>(), default);
    }
}
