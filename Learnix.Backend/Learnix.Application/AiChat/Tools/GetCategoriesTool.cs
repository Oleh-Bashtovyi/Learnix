using System.Text.Json;
using Learnix.Application.AiChat.Abstractions.Models;
using Learnix.Application.AiChat.Queries.GetCategories;
using MediatR;

namespace Learnix.Application.AiChat.Tools;

public sealed class GetCategoriesTool(IMediator mediator) : IChatTool
{
    private static readonly string ParametersSchema = JsonSerializer.Serialize(new
    {
        type = "object",
        properties = new { },
        required = Array.Empty<string>()
    });

    public string Name => "get_categories";

    public ToolDefinition Definition => new(
        Name: "get_categories",
        Description: "Returns all available course categories with their slugs and course counts. " +
                     "Call this when the user mentions a subject area and you need the correct category slug " +
                     "to pass to search_courses.",
        ParametersJsonSchema: ParametersSchema);

    public async Task<string> ExecuteAsync(string argumentsJson, CancellationToken ct)
    {
        var result = await mediator.Send(new GetCategoriesQuery(), ct);

        if (result.IsFailed)
            return JsonSerializer.Serialize(new { error = "Failed to retrieve categories" });

        return JsonSerializer.Serialize(new { categories = result.Value });
    }
}
