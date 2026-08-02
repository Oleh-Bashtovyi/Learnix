using System.Text.Json;
using Learnix.Application.AiChat.Abstractions.Models;
using Learnix.Application.AiChat.Constants;
using Learnix.Application.Courses.Queries.GetPublishedCourseCount;
using MediatR;

namespace Learnix.Application.AiChat.Tools;

public sealed class GetPlatformStatsTool(IMediator mediator) : IChatTool
{
    private static readonly string ParametersSchema = JsonSerializer.Serialize(new
    {
        type = "object",
        properties = new { },
        required = Array.Empty<string>()
    });

    public string Name => ChatToolNames.GetPlatformStats;

    public ToolDefinition Definition => new(
        Name: Name,
        Description: "Returns platform-wide statistics, currently the total number of published courses. " +
                     "Call this when the user asks how many courses the platform offers.",
        ParametersJsonSchema: ParametersSchema);

    public bool IsAvailableIn(ChatScopeType scope) => scope is ChatScopeType.Platform;

    public async Task<string> ExecuteAsync(ChatToolInvocation invocation, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetPublishedCourseCountQuery(), cancellationToken);

        if (result.IsFailed)
            return JsonSerializer.Serialize(new { error = "Failed to retrieve platform statistics" });

        return JsonSerializer.Serialize(new { publishedCourseCount = result.Value });
    }
}
