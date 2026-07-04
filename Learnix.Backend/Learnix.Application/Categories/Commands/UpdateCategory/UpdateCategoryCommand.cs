using FluentResults;
using MediatR;

namespace Learnix.Application.Categories.Commands.UpdateCategory;

public sealed record UpdateCategoryCommand(Guid CategoryId, string Name, string Slug) : IRequest<Result>;
