using Learnix.Application.Certificates.Abstractions;
using Learnix.Application.Certificates.Queries.GetMyCertificates;
using Learnix.Application.Certificates.Specifications;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Storage;
using Learnix.Application.Common.Constants;
using Learnix.Application.Common.Errors;
using Learnix.Application.Common.Options;
using Learnix.Domain.Entities;
using Microsoft.Extensions.Options;

namespace Learnix.Application.UnitTests.Certificates.Queries.GetMyCertificates;

public class GetMyCertificatesQueryHandlerTests
{
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly ICertificateRepository _certificateRepository = Substitute.For<ICertificateRepository>();
    private readonly IBlobStorageService _blobStorageService = Substitute.For<IBlobStorageService>();
    private readonly IOptions<AppOptions> _appSettings = Substitute.For<IOptions<AppOptions>>();
    private readonly GetMyCertificatesQueryHandler _sut;

    public GetMyCertificatesQueryHandlerTests()
    {
        _sut = new GetMyCertificatesQueryHandler(
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
        var query = new GetMyCertificatesQuery();

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.HasError<AuthenticationError>().Should().BeTrue();
        result.Errors[0].Message.Should().Be(CommonMessages.NotAuthenticated);
    }

    [Fact]
    public async Task Handle_ShouldReturnCertificates_WhenSuccessful()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        _currentUserService.UserId.Returns(studentId);

        var category = Category.CreateSystem("Programming", "programming");
        var course = Course.Create(Guid.NewGuid(), category.Id, "Title", "Desc", 0m);
        course.SetCoverImage("path/to/cover.jpg");

        var enrollment = Enrollment.Create(course.Id, studentId, course.Price);

        enrollment.MarkCompleted();
        var certificate = Certificate.Issue(enrollment, course);
        typeof(Certificate).GetProperty(nameof(Certificate.Course))?.SetValue(certificate, course);
        certificate.AttachFile("path/to/cert.pdf");

        _certificateRepository.ListAsync(Arg.Any<CertificatesByStudentSpecification>(), Arg.Any<CancellationToken>())
            .Returns(new List<Certificate> { certificate });

        _blobStorageService.GetPublicUrl("path/to/cover.jpg").Returns("http://storage.com/cover.jpg");
        _blobStorageService.GenerateReadUrl("path/to/cert.pdf", Arg.Any<TimeSpan>()).Returns("http://storage.com/cert.pdf");

        var query = new GetMyCertificatesQuery();

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);

        var dto = result.Value[0];
        dto.CourseId.Should().Be(course.Id);
        dto.CourseTitle.Should().Be("Title");
        dto.CourseCoverBlobPath.Should().Be("http://storage.com/cover.jpg");
        dto.Code.Should().Be(certificate.Code);
        dto.IsReady.Should().BeTrue();
        dto.DownloadUrl.Should().Be("http://storage.com/cert.pdf");
        dto.VerificationUrl.Should().Be($"http://client.com/verify/{certificate.Code}");
    }
}
