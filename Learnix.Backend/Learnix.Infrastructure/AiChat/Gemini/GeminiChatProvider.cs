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

    public async IAsyncEnumerable<ChatStreamEvent> StreamChatAsync(
        IReadOnlyList<ChatMessage> conversation,
        IReadOnlyList<ToolDefinition> tools,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var contents = MapContents(conversation);
        var config = BuildConfig(tools);
        string? finishReason = null;

        await foreach (var chunk in _client.Models
            .GenerateContentStreamAsync(_settings.Model, contents, config)
            .WithCancellation(ct))
        {
            if (chunk.Candidates is null) continue;

            foreach (var candidate in chunk.Candidates)
            {
                if (candidate.Content?.Parts is not null)
                {
                    foreach (var part in candidate.Content.Parts)
                    {
                        if (part.Text is not null)
                            yield return new TextDeltaEvent(part.Text);

                        if (part.FunctionCall is not null)
                        {
                            var callId = Guid.NewGuid().ToString("N")[..8];
                            var argsJson = JsonSerializer.Serialize(part.FunctionCall.Args);
                            var toolName = part.FunctionCall.Name ?? string.Empty;
                            yield return new ToolUseStartEvent(callId, toolName);
                            yield return new ToolUseEndEvent(callId, toolName, argsJson);
                        }
                    }
                }

                if (candidate.FinishReason is not null)
                    finishReason = candidate.FinishReason.ToString();
            }
        }

        yield return new MessageEndEvent(finishReason ?? "stop");
    }

    private static List<Content> MapContents(IReadOnlyList<ChatMessage> conversation)
    {
        var contents = new List<Content>();

        foreach (var msg in conversation)
        {
            if (msg.Role == "tool_result")
            {
                var parts = msg.ToolCalls!.Select(tc =>
                {
                    var response = tc.ResultJson is not null
                        ? JsonSerializer.Deserialize<Dictionary<string, object>>(tc.ResultJson)
                        : new Dictionary<string, object>();

                    return new Part
                    {
                        FunctionResponse = new FunctionResponse
                        {
                            Name = tc.ToolName,
                            Response = response
                        }
                    };
                }).ToList();

                contents.Add(new Content { Role = "user", Parts = parts });
            }
            else if (msg.Role == "assistant" && msg.ToolCalls is { Count: > 0 })
            {
                var parts = new List<Part>();

                if (!string.IsNullOrEmpty(msg.Content))
                    parts.Add(new Part { Text = msg.Content });

                foreach (var tc in msg.ToolCalls)
                {
                    var args = tc.ArgumentsJson.Length > 0
                        ? JsonSerializer.Deserialize<Dictionary<string, object>>(tc.ArgumentsJson)
                        : new Dictionary<string, object>();

                    parts.Add(new Part
                    {
                        FunctionCall = new FunctionCall { Name = tc.ToolName, Args = args }
                    });
                }

                contents.Add(new Content { Role = "model", Parts = parts });
            }
            else
            {
                var role = msg.Role == "assistant" ? "model" : "user";
                contents.Add(new Content { Role = role, Parts = [new Part { Text = msg.Content }] });
            }
        }

        return contents;
    }

    private GenerateContentConfig BuildConfig(IReadOnlyList<ToolDefinition> tools)
    {
        var config = new GenerateContentConfig
        {
            SystemInstruction = new Content
            {
                Parts = [new Part { Text = AiChatConstants.SystemPrompt }]
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
