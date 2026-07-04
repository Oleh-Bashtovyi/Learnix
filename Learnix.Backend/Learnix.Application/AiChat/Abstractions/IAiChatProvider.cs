using Learnix.Application.AiChat.Abstractions.Models;

namespace Learnix.Application.AiChat.Abstractions;

public interface IAiChatProvider
{
    IAsyncEnumerable<ChatStreamEvent> StreamChatAsync(
        IReadOnlyList<ChatMessage> conversation,
        IReadOnlyList<ToolDefinition> tools,
        CancellationToken ct);
}
