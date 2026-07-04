using FluentResults;
using Learnix.Application.Common.Abstractions.Storage;
using MediatR;

namespace Learnix.Application.Uploads.Commands.RequestUploadUrl;

public record RequestUploadUrlCommand(
    UploadTarget Target,
    string ContentType
) : IRequest<Result<UploadUrlResponse>>;
