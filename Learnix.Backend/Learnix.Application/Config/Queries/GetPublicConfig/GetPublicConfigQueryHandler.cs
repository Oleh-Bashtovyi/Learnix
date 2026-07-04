using FluentResults;
using Learnix.Application.Common.Settings;
using MediatR;
using Microsoft.Extensions.Options;

namespace Learnix.Application.Config.Queries.GetPublicConfig;

internal sealed class GetPublicConfigQueryHandler(IOptions<AiChatSettings> aiChatSettings)
    : IRequestHandler<GetPublicConfigQuery, Result<PublicConfigDto>>
{
    public Task<Result<PublicConfigDto>> Handle(
        GetPublicConfigQuery request,
        CancellationToken cancellationToken)
    {
        var dto = new PublicConfigDto(aiChatSettings.Value.Provider);
        return Task.FromResult(Result.Ok(dto));
    }
}
