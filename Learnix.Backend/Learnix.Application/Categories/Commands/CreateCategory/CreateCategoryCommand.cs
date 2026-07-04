using FluentResults;
using MediatR;

namespace Learnix.Application.Categories.Commands.CreateCategory;

public sealed record CreateCategoryCommand(string Name, string Slug) : IRequest<Result<Guid>>;
