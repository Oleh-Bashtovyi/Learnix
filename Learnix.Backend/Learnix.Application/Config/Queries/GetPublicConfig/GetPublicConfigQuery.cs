using FluentResults;
using MediatR;

namespace Learnix.Application.Config.Queries.GetPublicConfig;

public sealed record GetPublicConfigQuery : IRequest<Result<PublicConfigDto>>;
