using System.Runtime.CompilerServices;
using System.Text.Json;
using Google.GenAI;
using Google.GenAI.Types;
using Learnix.Application.AiChat.Abstractions;
using Learnix.Application.AiChat.Abstractions.Models;
using Microsoft.Extensions.Options;
using ChatMessage = Learnix.Application.AiChat.Abstractions.Models.ChatMessage;

namespace Learnix.Infrastructure.AiChat.Gemini;

internal sealed class GeminiChatProvider : IAiChatProvider
{
    private readonly Client _client;
    private readonly GeminiSettings _settings;

    public GeminiChatProvider(IOptions<GeminiSettings> options)
    {
        _settings = options.Value;
        _client = new Client(apiKey: _settings.ApiKey);
    }

    public string Name => "Gemini";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_settings.ApiKey);

    /// <summary>
    /// The stream is driven by hand rather than with <c>await foreach</c>: a quota refusal surfaces as an
    /// exception out of <c>MoveNextAsync</c>, and an iterator cannot yield from inside a catch. Classifying
    /// it into a <see cref="ProviderErrorEvent"/> is the whole point — a throw here would just kill an SSE
    /// connection whose headers are already out (ADR-CHAT-014).
    /// </summary>
    public async IAsyncEnumerable<ChatStreamEvent> StreamChatAsync(
        ChatRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var contents = MapContents(request.Conversation);
        var config = BuildConfig(request.Tools, request.SystemPrompt);
        string? finishReason = null;

        var chunks = _client.Models
            .GenerateContentStreamAsync(_settings.Model, contents, config, cancellationToken)
            .GetAsyncEnumerator(cancellationToken);

        try
        {
            while (true)
            {
                List<ChatStreamEvent> events;
                ChatStreamEvent? failure = null;

                try
                {
                    if (!await chunks.MoveNextAsync())
                        break;

                    events = MapChunk(chunks.Current, ref finishReason);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    events = [];
                    failure = AiProviderErrors.Classify(ex);
                }

                if (failure is not null)
                {
                    yield return failure;
                    yield break;
                }

                foreach (var streamEvent in events)
                    yield return streamEvent;
            }
        }
        finally
        {
            await chunks.DisposeAsync();
        }

        yield return new MessageEndEvent(finishReason ?? "stop");
    }

    private static List<ChatStreamEvent> MapChunk(GenerateContentResponse chunk, ref string? finishReason)
    {
        var events = new List<ChatStreamEvent>();

        if (chunk.Candidates is null)
            return events;

        foreach (var candidate in chunk.Candidates)
        {
            foreach (var part in candidate.Content?.Parts ?? [])
                events.AddRange(MapPart(part));

            if (candidate.FinishReason is not null)
                finishReason = candidate.FinishReason.ToString();
        }

        return events;
    }

    /// <summary>A part carries either streamed text or a tool call — a tool call becomes a start/end pair.</summary>
    private static IEnumerable<ChatStreamEvent> MapPart(Part part)
    {
        if (part.Text is not null)
            yield return new TextDeltaEvent(part.Text);

        if (part.FunctionCall is null)
            yield break;

        var callId = Guid.NewGuid().ToString("N")[..8];
        var toolName = part.FunctionCall.Name ?? string.Empty;
        var argsJson = JsonSerializer.Serialize(part.FunctionCall.Args);

        yield return new ToolUseStartEvent(callId, toolName);
        yield return new ToolUseEndEvent(callId, toolName, argsJson);
    }

    private static List<Content> MapContents(IReadOnlyList<ChatMessage> conversation)
        => [.. conversation.Select(MapMessage)];

    /// <summary>
    /// Gemini has no "tool" role: tool results are sent back as a user turn of FunctionResponse parts,
    /// and the assistant's own tool calls as a model turn of FunctionCall parts.
    /// </summary>
    private static Content MapMessage(ChatMessage message)
    {
        if (message.Role == "tool_result")
        {
            var parts = message.ToolCalls!
                .Select(tc => new Part
                {
                    FunctionResponse = new FunctionResponse
                    {
                        Name = tc.ToolName,
                        Response = DeserializeArgs(tc.ResultJson)
                    }
                })
                .ToList();

            return new Content { Role = "user", Parts = parts };
        }

        if (message.Role == "assistant" && message.ToolCalls is { Count: > 0 })
        {
            var parts = new List<Part>();

            if (!string.IsNullOrEmpty(message.Content))
                parts.Add(new Part { Text = message.Content });

            parts.AddRange(message.ToolCalls.Select(tc => new Part
            {
                FunctionCall = new FunctionCall
                {
                    Name = tc.ToolName,
                    Args = DeserializeArgs(tc.ArgumentsJson)
                }
            }));

            return new Content { Role = "model", Parts = parts };
        }

        return new Content
        {
            Role = message.Role == "assistant" ? "model" : "user",
            Parts = [new Part { Text = message.Content }]
        };
    }

    private static Dictionary<string, object>? DeserializeArgs(string? json)
        => string.IsNullOrEmpty(json)
            ? new Dictionary<string, object>()
            : JsonSerializer.Deserialize<Dictionary<string, object>>(json);

    private GenerateContentConfig BuildConfig(IReadOnlyList<ToolDefinition> tools, string systemPrompt)
    {
        var config = new GenerateContentConfig
        {
            SystemInstruction = new Content
            {
                Parts = [new Part { Text = systemPrompt }]
            },
            MaxOutputTokens = _settings.MaxTokens
        };

        if (tools.Count > 0)
        {
            var declarations = tools.Select(t => new FunctionDeclaration
            {
                Name = t.Name,
                Description = t.Description,
                Parameters = JsonSerializer.Deserialize<Schema>(t.ParametersJsonSchema)
            }).ToList();

            config.Tools = [new Tool { FunctionDeclarations = declarations }];
        }

        return config;
    }
}
