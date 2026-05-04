using FluentResults;
using MediatR;

namespace Learnix.Application.Categories.Commands.SetCategoryImage;

public sealed record SetCategoryImageCommand(
    Guid CategoryId,
    string BlobPath) : IRequest<Result>;
