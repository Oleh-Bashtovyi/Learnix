using FluentResults;
using Learnix.Application.Certificates.Abstractions;
using Learnix.Application.Certificates.Constants;
using Learnix.Application.Certificates.Specifications;
using Learnix.Application.Common.Abstractions.Storage;
using Learnix.Application.Common.Constants;
using Learnix.Application.Common.Errors;
using Learnix.Application.Users.Abstractions;
using MediatR;

namespace Learnix.Application.Certificates.Queries.VerifyCertificate;

public sealed class VerifyCertificateQueryHandler(
    ICertificateRepository certificateRepository,
    IUserRepository userRepository,
    IBlobStorageService blobStorageService)
    : IRequestHandler<VerifyCertificateQuery, Result<VerifyCertificateResponse>>
{
    public async Task<Result<VerifyCertificateResponse>> Handle(
        VerifyCertificateQuery request,
        CancellationToken cancellationToken)
    {
        var certificate = await certificateRepository.FirstOrDefaultAsync(
            new CertificateByCodeSpecification(request.Code),
            cancellationToken);

        if (certificate is null)
            return Result.Fail(new NotFoundError(CertificateMessages.NotFoundOrInvalidCode));

        if (certificate.Course is null)
            return Result.Fail(new NotFoundError(CertificateMessages.CourseAssociatedNotFound));

        var student = await userRepository.GetByIdAsync(certificate.StudentId, cancellationToken);
        var instructor = await userRepository.GetByIdAsync(certificate.Course.InstructorId, cancellationToken);

        string? downloadUrl = null;
        if (certificate.FileUrl is not null)
            downloadUrl = blobStorageService.GenerateReadUrl(certificate.FileUrl, BlobUrlTtlConstants.CertificateReadUrl);

        return Result.Ok(new VerifyCertificateResponse(
            certificate.Code,
            certificate.Course.Title,
            student?.FirstName ?? "Unknown",
            student?.LastName ?? "Student",
            instructor?.FirstName ?? "Unknown",
            instructor?.LastName ?? "Instructor",
            certificate.IssuedAt,
            IsReady: certificate.FileUrl is not null,
            DownloadUrl: downloadUrl));
    }
}
