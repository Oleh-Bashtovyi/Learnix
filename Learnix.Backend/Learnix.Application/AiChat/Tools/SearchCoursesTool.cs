using System.Text.Json;
using System.Text.Json.Serialization;
using Learnix.Application.AiChat.Abstractions.Models;
using Learnix.Application.AiChat.Constants;
using Learnix.Application.AiChat.Queries.SearchCourses;
using MediatR;

namespace Learnix.Application.AiChat.Tools;

public sealed class SearchCoursesTool(IMediator mediator) : IChatTool
{
    private static readonly JsonSerializerOptions ArgumentsOptions = new() { PropertyNameCaseInsensitive = true };

    private static readonly string ParametersSchema = JsonSerializer.Serialize(new
    {
        type = "object",
        properties = new
        {
            query = new
            {
                type = "string",
                description =
                    "One to three English keywords naming the SUBJECT, e.g. 'python', 'data analysis', 'git'. "
                    + "Do not pass the user's sentence, and do not include words like 'course', 'courses' or "
                    + "'tutorial' — every course is a course, so those words only narrow the search for no reason."
            },
            category = new { type = "string", description = "Category slug to filter courses (optional)" },
            maxResults = new { type = "integer", description = "Maximum number of results (1-20, default 10)", @default = 10 }
        },
        required = new[] { "query" }
    });

    public string Name => ChatToolNames.SearchCourses;

    public ToolDefinition Definition => new(
        Name: Name,
        Description: "Search published courses by keyword and optional category. Use when user asks for course recommendations.",
        ParametersJsonSchema: ParametersSchema);

    public bool IsAvailableIn(ChatScopeType scope) => scope is ChatScopeType.Platform;

    public async Task<string> ExecuteAsync(ChatToolInvocation invocation, CancellationToken cancellationToken)
    {
        var argumentsJson = invocation.ArgumentsJson;
        SearchArgs? args = null;
        try
        {
            args = JsonSerializer.Deserialize<SearchArgs>(argumentsJson, ArgumentsOptions);
        }
        catch (JsonException)
        {
            return JsonSerializer.Serialize(new { error = "Invalid arguments" });
        }

        if (args is null || string.IsNullOrWhiteSpace(args.Query))
            return JsonSerializer.Serialize(new { error = "Missing required parameter: query" });

        var result = await mediator.Send(
            new SearchCoursesQuery(args.Query, args.Category, args.MaxResults ?? 10),
            cancellationToken);

        if (result.IsFailed)
            return JsonSerializer.Serialize(new { error = "Search failed" });

        return JsonSerializer.Serialize(new { courses = result.Value });
    }

    private sealed record SearchArgs(
        [property: JsonPropertyName("query")] string Query,
        [property: JsonPropertyName("category")] string? Category,
        [property: JsonPropertyName("maxResults")] int? MaxResults);
}
