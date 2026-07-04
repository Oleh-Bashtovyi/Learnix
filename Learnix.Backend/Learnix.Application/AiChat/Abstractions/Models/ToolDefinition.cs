namespace Learnix.Application.AiChat.Abstractions.Models;

public sealed record ToolDefinition(
    string Name,
    string Description,
    string ParametersJsonSchema);
