using FluentResults;
using MediatR;

namespace Learnix.Application.AiChat.Queries.GetCategories;

public sealed record GetCategoriesQuery : IRequest<Result<IReadOnlyList<CategoryAiDto>>>;
