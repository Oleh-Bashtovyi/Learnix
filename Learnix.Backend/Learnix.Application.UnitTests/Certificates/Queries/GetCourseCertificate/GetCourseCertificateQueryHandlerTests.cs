using Learnix.Application.Certificates.Abstractions;
using Learnix.Application.Certificates.Constants;
using Learnix.Application.Certificates.Queries.GetCourseCertificate;
using Learnix.Application.Certificates.Specifications;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Storage;
using Learnix.Application.Common.Constants;
using Learnix.Application.Common.Errors;
using Learnix.Application.Common.Options;
using Learnix.Domain.Entities;
using Microsoft.Extensions.Options;
using NSubstitute.ReturnsExtensions;

namespace Learnix.Application.UnitTests.Certificates.Queries.GetCourseCertificate;

public class GetCourseCertificateQueryHandlerTests
{
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly ICertificateRepository _certificateRepository = Substitute.For<ICertificateRepository>();
    private readonly IBlobStorageService _blobStorageService = Substitute.For<IBlobStorageService>();
    private readonly IOptions<AppOptions> _appSettings = Substitute.For<IOptions<AppOptions>>();
    private readonly GetCourseCertificateQueryHandler _sut;

    public GetCourseCertificateQueryHandlerTests()
    {
        _sut = new GetCourseCertificateQueryHandler(
            _currentUserService,
            _certificateRepository,
            _blobStorageService,
            _appSettings);

        _appSettings.Value.Returns(new AppOptions { ClientBaseUrl = "http://client.com" });
    }

    [Fact]
    public async Task Handle_ShouldReturnError_WhenUserIsNotAuthenticated()
    {
        // Arrange
        _currentUserService.UserId.Returns((Guid?)null);
        var query = new GetCourseCertificateQuery(Guid.NewGuid());

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.HasError<AuthenticationError>().Should().BeTrue();
        result.Errors[0].Message.Should().Be(CommonMessages.NotAuthenticated);
    }

    [Fact]
    public async Task Handle_ShouldReturnError_WhenCertificateNotFound()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        _currentUserService.UserId.Returns(studentId);

        _certificateRepository.FirstOrDefaultAsync(Arg.Any<CertificateByCourseAndStudentSpecification>(), Arg.Any<CancellationToken>())
            .ReturnsNull();

        var query = new GetCourseCertificateQuery(courseId);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.HasError<NotFoundError>().Should().BeTrue();
        result.Errors[0].Message.Should().Be(CertificateMessages.NotFoundCompleteLessons);
    }

    [Fact]
    public async Task Handle_ShouldReturnCertificateWithoutDownloadUrl_WhenNotReady()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        _currentUserService.UserId.Returns(studentId);

        var category = Category.CreateSystem("Programming", "programming");
        var course = Course.Create(Guid.NewGuid(), category.Id, "Title", "Desc", 0m);
        var enrollment = Enrollment.Create(course.Id, studentId, course.Price);
        enrollment.MarkCompleted();
        var certificate = Certificate.Issue(enrollment, course);

        _certificateRepository.FirstOrDefaultAsync(Arg.Any<CertificateByCourseAndStudentSpecification>(), Arg.Any<CancellationToken>())
            .Returns(certificate);

        var query = new GetCourseCertificateQuery(courseId);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsReady.Should().BeFalse();
        result.Value.DownloadUrl.Should().BeNull();
        result.Value.VerificationUrl.Should().Be($"http://client.com/verify/{certificate.Code}");
    }

    [Fact]
    public async Task Handle_ShouldReturnCertificateWithDownloadUrl_WhenReady()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        _currentUserService.UserId.Returns(studentId);

        var category = Category.CreateSystem("Programming", "programming");
        var course = Course.Create(Guid.NewGuid(), category.Id, "Title", "Desc", 0m);
        var enrollment = Enrollment.Create(course.Id, studentId, course.Price);
        enrollment.MarkCompleted();
        var certificate = Certificate.Issue(enrollment, course);
        certificate.AttachFile("path/to/cert.pdf");

        _certificateRepository.FirstOrDefaultAsync(Arg.Any<CertificateByCourseAndStudentSpecification>(), Arg.Any<CancellationToken>())
            .Returns(certificate);

        _blobStorageService.GenerateReadUrl("path/to/cert.pdf", Arg.Any<TimeSpan>())
            .Returns("http://storage.com/cert.pdf");

        var query = new GetCourseCertificateQuery(courseId);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsReady.Should().BeTrue();
        result.Value.DownloadUrl.Should().Be("http://storage.com/cert.pdf");
        result.Value.VerificationUrl.Should().Be($"http://client.com/verify/{certificate.Code}");
    }
}
