using FluentResults;
using MediatR;

namespace Learnix.Application.Categories.Commands.DeleteCategory;

public sealed record DeleteCategoryCommand(Guid CategoryId) : IRequest<Result>;
