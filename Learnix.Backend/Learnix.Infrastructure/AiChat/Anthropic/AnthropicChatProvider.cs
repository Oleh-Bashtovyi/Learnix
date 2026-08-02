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
    IOptions<AnthropicOptions> options) : IAiChatProvider
{
    public string Name => "Anthropic";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(options.Value.ApiKey);

    /// <summary>
    /// Driven by hand rather than with <c>await foreach</c>: a rate limit or a rejected key surfaces as an
    /// exception out of <c>MoveNextAsync</c>, and an iterator cannot yield from inside a catch. See
    /// <see cref="AiProviderErrors"/> and ADR-BACK-CHAT-014.
    /// </summary>
    public async IAsyncEnumerable<ChatStreamEvent> StreamChatAsync(
        ChatRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var outputs = new List<MessageResponse>();
        IAsyncEnumerator<MessageResponse>? responses = null;
        ChatStreamEvent? setupFailure = null;

        // Building the request touches stored history — BuildMessages parses ArgumentsJson for every past
        // tool call — so a malformed row is a real possibility, not just a defensive guard. Left outside
        // this try, it would throw straight out of the iterator with SSE headers already flushed (see
        // AiChatController.StreamMessage), the same failure mode ADR-BACK-CHAT-014 exists to prevent for
        // the provider call itself.
        try
        {
            var parameters = new MessageParameters
            {
                Model = options.Value.Model,
                MaxTokens = options.Value.MaxTokens,
                Stream = true,
                System = [new SystemMessage(request.SystemPrompt)],
                Messages = BuildMessages(request.Conversation),
                Tools = request.Tools.Count > 0 ? BuildTools(request.Tools) : null
            };
            responses = client.Messages.StreamClaudeMessageAsync(parameters, cancellationToken).GetAsyncEnumerator(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            setupFailure = AiProviderErrors.Classify(ex);
        }

        if (setupFailure is not null)
        {
            yield return setupFailure;
            yield break;
        }

        try
        {
            while (true)
            {
                MessageResponse? res = null;
                ChatStreamEvent? failure = null;

                try
                {
                    if (!await responses!.MoveNextAsync())
                        break;

                    res = responses.Current;
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    failure = AiProviderErrors.Classify(ex);
                }

                if (failure is not null)
                {
                    yield return failure;
                    yield break;
                }

                if (res!.Delta?.Text is not null)
                    yield return new TextDeltaEvent(res.Delta.Text);

                outputs.Add(res);
            }
        }
        finally
        {
            if (responses is not null)
                await responses.DisposeAsync();
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

        // The final message_delta chunk of the stream carries the real stop reason ("end_turn",
        // "max_tokens", "tool_use", ...) on its Delta — earlier chunks only ever carry text.
        var stopReason = outputs
            .Select(r => r.Delta?.StopReason)
            .LastOrDefault(r => !string.IsNullOrEmpty(r));

        yield return new MessageEndEvent(stopReason ?? "end_turn");
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
