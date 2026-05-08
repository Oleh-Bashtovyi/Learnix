using FluentResults;
using Learnix.Application.Certificates.Abstractions;
using Learnix.Application.Certificates.Specifications;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Storage;
using Learnix.Application.Common.Constants;
using Learnix.Application.Common.Errors;
using Learnix.Application.Common.Settings;
using MediatR;
using Microsoft.Extensions.Options;

namespace Learnix.Application.Certificates.Queries.GetMyCertificates;

public sealed class GetMyCertificatesQueryHandler(
    ICurrentUserService currentUser,
    ICertificateRepository certificateRepository,
    IBlobStorageService blobStorageService,
    IOptions<AppSettings> appSettings)
    : IRequestHandler<GetMyCertificatesQuery, Result<IReadOnlyList<MyCertificateDto>>>
{
    public async Task<Result<IReadOnlyList<MyCertificateDto>>> Handle(
        GetMyCertificatesQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Result.Fail(new AuthenticationError(CommonMessages.NotAuthenticated));

        var studentId = currentUser.UserId.Value;

        var certificates = await certificateRepository.ListAsync(
            new CertificatesByStudentSpecification(studentId),
            cancellationToken);

        var dtos = certificates.Select(c =>
        {
            string? downloadUrl = c.FileUrl is not null
                ? blobStorageService.GenerateReadUrl(c.FileUrl, TimeSpan.FromHours(24))
                : null;

            return new MyCertificateDto(
                c.Id,
                c.CourseId,
                c.Course!.Title,
                c.Course.CoverBlobPath,
                c.Code,
                c.IssuedAt,
                IsReady: c.FileUrl is not null,
                DownloadUrl: downloadUrl,
                VerificationUrl: $"{appSettings.Value.ClientBaseUrl}/verify/{c.Code}");
        }).ToList();

        return Result.Ok<IReadOnlyList<MyCertificateDto>>(dtos);
    }
}
