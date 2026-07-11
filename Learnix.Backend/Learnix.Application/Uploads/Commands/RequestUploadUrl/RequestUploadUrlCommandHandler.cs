using FluentResults;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Storage;
using Learnix.Application.Common.Constants;
using Learnix.Application.Common.Errors;
using Learnix.Application.Uploads.Constants;
using Learnix.Domain.Constants;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Learnix.Application.Uploads.Commands.RequestUploadUrl;

public sealed class RequestUploadUrlCommandHandler(
    IBlobStorageService blobStorage,
    ICurrentUserService currentUser,
    ILogger<RequestUploadUrlCommandHandler> logger
) : IRequestHandler<RequestUploadUrlCommand, Result<UploadUrlResponse>>
{
    public async Task<Result<UploadUrlResponse>> Handle(
        RequestUploadUrlCommand request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Result.Fail(new AuthenticationError(CommonMessages.NotAuthenticated));

        // Course material follows course authorization, which is owner-or-admin everywhere else.
        // An admin who may edit somebody else's course must be able to upload into it, and the URL alone
        // grants nothing — the blob is only claimed by a handler that re-checks ownership.
        var canUploadCourseMaterial =
            currentUser.IsInRole(Roles.Instructor) || currentUser.IsInRole(Roles.Admin);

        if (request.Target == UploadTarget.LessonVideo && !canUploadCourseMaterial)
            return Result.Fail(new ForbiddenError(UploadMessages.OnlyInstructorsUploadVideos));

        if (request.Target == UploadTarget.CourseCover && !canUploadCourseMaterial)
            return Result.Fail(new ForbiddenError(UploadMessages.OnlyInstructorsUploadCovers));

        // No role at all: a certificate is the platform's own signature, and one uploaded by hand would be
        // indistinguishable from one it issued.
        if (request.Target == UploadTarget.Certificate)
            return Result.Fail(new ForbiddenError(UploadMessages.CertificatesGeneratedBySystem));

        if (request.Target == UploadTarget.CategoryImage && !currentUser.IsInRole(Roles.Admin))
            return Result.Fail(new ForbiddenError(UploadMessages.OnlyAdminsUploadCategoryImages));

        var response = await blobStorage.GenerateUploadUrlAsync(
            request.Target,
            request.ContentType,
            cancellationToken);

        logger.LogInformation(
            "Upload URL generated for user {UserId}, target {Target}, path {Path}",
            currentUser.UserId, request.Target, response.BlobPath);

        return Result.Ok(response);
    }
}
