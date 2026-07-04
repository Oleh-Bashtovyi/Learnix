using Learnix.Application.AiChat.Abstractions.Models;

namespace Learnix.Application.AiChat.Tools;

public interface IChatTool
{
    string Name { get; }
    ToolDefinition Definition { get; }
    Task<string> ExecuteAsync(string argumentsJson, CancellationToken ct);
}
