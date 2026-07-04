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
        CancellationToken ct)
    {
        if (currentUser.UserId is null)
            return Result.Fail(new AuthenticationError(CommonMessages.NotAuthenticated));

        if (request.Target == UploadTarget.LessonVideo && !currentUser.IsInRole(Roles.Instructor))
            return Result.Fail(new ForbiddenError(UploadMessages.OnlyInstructorsUploadVideos));

        if (request.Target == UploadTarget.CourseCover && !currentUser.IsInRole(Roles.Instructor))
            return Result.Fail(new ForbiddenError(UploadMessages.OnlyInstructorsUploadCovers));

        if (request.Target == UploadTarget.Certificate)
            return Result.Fail(new ForbiddenError(UploadMessages.CertificatesGeneratedBySystem));

        if (request.Target == UploadTarget.CategoryImage && !currentUser.IsInRole(Roles.Admin))
            return Result.Fail(new ForbiddenError(UploadMessages.OnlyAdminsUploadCategoryImages));

        var response = await blobStorage.GenerateUploadUrlAsync(
            request.Target,
            request.ContentType,
            ct);

        logger.LogInformation(
            "Upload URL generated for user {UserId}, target {Target}, path {Path}",
            currentUser.UserId, request.Target, response.BlobPath);

        return Result.Ok(response);
    }
}
