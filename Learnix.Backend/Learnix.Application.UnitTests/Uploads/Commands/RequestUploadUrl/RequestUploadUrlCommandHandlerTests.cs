using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Storage;
using Learnix.Application.Common.Errors;
using Learnix.Application.Uploads.Commands.RequestUploadUrl;
using Learnix.Domain.Constants;
using Microsoft.Extensions.Logging.Abstractions;

namespace Learnix.Application.UnitTests.Uploads.Commands.RequestUploadUrl;

public class RequestUploadUrlCommandHandlerTests
{
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly IBlobStorageService _blobStorage = Substitute.For<IBlobStorageService>();
    private readonly RequestUploadUrlCommandHandler _sut;

    public RequestUploadUrlCommandHandlerTests()
    {
        _currentUser.UserId.Returns(Guid.NewGuid());
        _blobStorage
            .GenerateUploadUrlAsync(Arg.Any<UploadTarget>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new UploadUrlResponse("https://blob/temp?sas", "temp-uploads/abc", DateTimeOffset.UtcNow));

        _sut = new RequestUploadUrlCommandHandler(
            _blobStorage, _currentUser, NullLogger<RequestUploadUrlCommandHandler>.Instance);
    }

    private Task<FluentResults.Result<UploadUrlResponse>> Act(UploadTarget target, string contentType = "image/webp") =>
        _sut.Handle(new RequestUploadUrlCommand(target, contentType), CancellationToken.None);

    [Fact]
    public async Task Any_signed_in_user_may_upload_their_own_avatar()
    {
        var result = await Act(UploadTarget.Avatar);

        result.IsSuccess.Should().BeTrue();
        result.Value.BlobPath.Should().Be("temp-uploads/abc");
    }

    [Theory]
    [InlineData(UploadTarget.LessonVideo)]
    [InlineData(UploadTarget.CourseCover)]
    public async Task A_plain_student_cannot_upload_course_material(UploadTarget target)
    {
        var result = await Act(target);

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().BeOfType<ForbiddenError>();
        await _blobStorage.DidNotReceiveWithAnyArgs().GenerateUploadUrlAsync(default, default!, default);
    }

    [Theory]
    [InlineData(UploadTarget.LessonVideo)]
    [InlineData(UploadTarget.CourseCover)]
    public async Task An_instructor_gets_a_url_for_course_material(UploadTarget target)
    {
        _currentUser.IsInRole(Roles.Instructor).Returns(true);

        var result = await Act(target, "video/mp4");

        result.IsSuccess.Should().BeTrue();
    }

    /// <summary>
    /// Course authorization is owner-or-admin everywhere else, so an admin editing somebody's course must be
    /// able to replace its cover or its video. The URL is not the permission — the handler that claims the
    /// blob re-checks ownership.
    /// </summary>
    [Theory]
    [InlineData(UploadTarget.LessonVideo)]
    [InlineData(UploadTarget.CourseCover)]
    public async Task An_admin_gets_a_url_for_course_material_too(UploadTarget target)
    {
        _currentUser.IsInRole(Roles.Admin).Returns(true);

        var result = await Act(target, "video/mp4");

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Category_images_belong_to_admins()
    {
        var forbidden = await Act(UploadTarget.CategoryImage);
        forbidden.IsFailed.Should().BeTrue();
        forbidden.Errors[0].Should().BeOfType<ForbiddenError>();

        _currentUser.IsInRole(Roles.Admin).Returns(true);
        var allowed = await Act(UploadTarget.CategoryImage);
        allowed.IsSuccess.Should().BeTrue();
    }

    /// <summary>
    /// A certificate is minted by the platform and is the only proof it issues. No role may upload one —
    /// not even an admin, whose signature would be indistinguishable from the real thing.
    /// </summary>
    [Fact]
    public async Task Nobody_uploads_a_certificate_not_even_an_admin()
    {
        _currentUser.IsInRole(Roles.Admin).Returns(true);
        _currentUser.IsInRole(Roles.Instructor).Returns(true);

        var result = await Act(UploadTarget.Certificate, "application/pdf");

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().BeOfType<ForbiddenError>();
        await _blobStorage.DidNotReceiveWithAnyArgs().GenerateUploadUrlAsync(default, default!, default);
    }

    [Fact]
    public async Task An_anonymous_request_gets_no_upload_url()
    {
        _currentUser.UserId.Returns((Guid?)null);

        var result = await Act(UploadTarget.Avatar);

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().BeOfType<AuthenticationError>();
        await _blobStorage.DidNotReceiveWithAnyArgs().GenerateUploadUrlAsync(default, default!, default);
    }
}
