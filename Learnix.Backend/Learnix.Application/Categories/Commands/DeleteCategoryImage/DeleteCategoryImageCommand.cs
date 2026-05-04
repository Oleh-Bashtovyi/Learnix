using FluentResults;
using MediatR;

namespace Learnix.Application.Categories.Commands.DeleteCategoryImage;

public sealed record DeleteCategoryImageCommand(Guid CategoryId) : IRequest<Result>;
