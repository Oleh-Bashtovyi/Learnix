using FluentResults;
using Learnix.Application.Certificates.Abstractions;
using Learnix.Application.Certificates.Constants;
using Learnix.Application.Certificates.Specifications;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Storage;
using Learnix.Application.Common.Constants;
using Learnix.Application.Common.Errors;
using Learnix.Application.Common.Settings;
using MediatR;
using Microsoft.Extensions.Options;

namespace Learnix.Application.Certificates.Queries.GetCourseCertificate;

public sealed class GetCourseCertificateQueryHandler(
    ICurrentUserService currentUser,
    ICertificateRepository certificateRepository,
    IBlobStorageService blobStorageService,
    IOptions<AppSettings> appSettings)
    : IRequestHandler<GetCourseCertificateQuery, Result<CourseCertificateResponse>>
{
    public async Task<Result<CourseCertificateResponse>> Handle(
        GetCourseCertificateQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Result.Fail(new AuthenticationError(CommonMessages.NotAuthenticated));

        var studentId = currentUser.UserId.Value;

        var certificate = await certificateRepository.FirstOrDefaultAsync(
            new CertificateByCourseAndStudentSpecification(studentId, request.CourseId),
            cancellationToken);

        if (certificate is null)
            return Result.Fail(new NotFoundError(CertificateMessages.NotFoundCompleteLessons));

        var verificationUrl = $"{appSettings.Value.ClientBaseUrl}/verify/{certificate.Code}";

        string? downloadUrl = null;
        if (certificate.FileUrl is not null)
            downloadUrl = blobStorageService.GenerateReadUrl(certificate.FileUrl, BlobUrlTtlConstants.CertificateReadUrl);

        return Result.Ok(new CourseCertificateResponse(
            certificate.Id,
            certificate.Code,
            certificate.IssuedAt,
            IsReady: certificate.FileUrl is not null,
            DownloadUrl: downloadUrl,
            VerificationUrl: verificationUrl));
    }
}
