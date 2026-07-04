using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;
using Anthropic.SDK;
using Anthropic.SDK.Common;
using Anthropic.SDK.Messaging;
using Learnix.Application.AiChat.Abstractions;
using Learnix.Application.AiChat.Abstractions.Models;
using Microsoft.Extensions.Options;
using AnthropicTool = Anthropic.SDK.Common.Tool;

namespace Learnix.Infrastructure.AiChat.Anthropic;

internal sealed class AnthropicChatProvider(
    AnthropicClient client,
    IOptions<AnthropicSettings> options) : IAiChatProvider
{
    public async IAsyncEnumerable<ChatStreamEvent> StreamChatAsync(
        IReadOnlyList<ChatMessage> conversation,
        IReadOnlyList<ToolDefinition> tools,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var parameters = new MessageParameters
        {
            Model = options.Value.Model,
            MaxTokens = options.Value.MaxTokens,
            Stream = true,
            System = [new SystemMessage(AiChatConstants.SystemPrompt)],
            Messages = BuildMessages(conversation),
            Tools = tools.Count > 0 ? BuildTools(tools) : null
        };

        var outputs = new List<MessageResponse>();

        await foreach (var res in client.Messages.StreamClaudeMessageAsync(parameters, ct))
        {
            if (res.Delta?.Text is not null)
                yield return new TextDeltaEvent(res.Delta.Text);
            outputs.Add(res);
        }

        // Tool use blocks are fully accumulated after streaming ends
        var assistantMsg = new Message(outputs);
        var toolBlocks = assistantMsg.Content
            ?.OfType<ToolUseContent>()
            .ToList() ?? [];

        foreach (var tc in toolBlocks)
        {
            yield return new ToolUseStartEvent(tc.Id, tc.Name);
            yield return new ToolUseEndEvent(tc.Id, tc.Name, tc.Input?.ToJsonString() ?? "{}");
        }

        yield return new MessageEndEvent("end_turn");
    }

    private static List<Message> BuildMessages(IReadOnlyList<ChatMessage> conversation)
    {
        var result = new List<Message>(conversation.Count);

        foreach (var msg in conversation)
        {
            if (msg.Role == "tool_result")
            {
                var blocks = msg.ToolCalls!
                    .Select(tc => (ContentBase)new ToolResultContent
                    {
                        ToolUseId = tc.CallId,
                        Content = [new TextContent { Text = tc.ResultJson ?? string.Empty }]
                    })
                    .ToList();
                result.Add(new Message { Role = RoleType.User, Content = blocks });
            }
            else if (msg.Role == "assistant" && msg.ToolCalls is { Count: > 0 })
            {
                var blocks = new List<ContentBase>();
                if (!string.IsNullOrEmpty(msg.Content))
                    blocks.Add(new TextContent { Text = msg.Content });
                foreach (var tc in msg.ToolCalls)
                    blocks.Add(new ToolUseContent
                    {
                        Id = tc.CallId,
                        Name = tc.ToolName,
                        Input = JsonNode.Parse(tc.ArgumentsJson)?.AsObject() ?? []
                    });
                result.Add(new Message { Role = RoleType.Assistant, Content = blocks });
            }
            else
            {
                result.Add(new Message(
                    msg.Role == "assistant" ? RoleType.Assistant : RoleType.User,
                    msg.Content));
            }
        }

        return result;
    }

    private static List<AnthropicTool> BuildTools(IReadOnlyList<ToolDefinition> tools) =>
        tools.Select(t => (AnthropicTool)new Function(t.Name, t.Description,
            JsonNode.Parse(t.ParametersJsonSchema))).ToList();
}
