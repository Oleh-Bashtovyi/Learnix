using FluentValidation;
using Learnix.Application.Common.Abstractions.Storage;

namespace Learnix.Application.Uploads.Commands.RequestUploadUrl;

public class RequestUploadUrlCommandValidator : AbstractValidator<RequestUploadUrlCommand>
{
    private static readonly Dictionary<UploadTarget, string[]> AllowedContentTypes = new()
    {
        [UploadTarget.Avatar] = ["image/jpeg", "image/png", "image/webp"],
        [UploadTarget.CourseCover] = ["image/jpeg", "image/png", "image/webp"],
        [UploadTarget.LessonVideo] = ["video/mp4", "video/webm"],
        [UploadTarget.CategoryImage] = ["image/jpeg", "image/png", "image/webp"],
    };

    public RequestUploadUrlCommandValidator()
    {
        RuleFor(x => x.Target)
            .IsInEnum();

        RuleFor(x => x.ContentType)
            .NotEmpty()
            .Must((cmd, ct) =>
                AllowedContentTypes.TryGetValue(cmd.Target, out var allowed)
                && allowed.Contains(ct))
            .WithMessage(cmd =>
                $"Content type must be one of: {string.Join(", ", AllowedContentTypes.GetValueOrDefault(cmd.Target, []))}");
    }
}
