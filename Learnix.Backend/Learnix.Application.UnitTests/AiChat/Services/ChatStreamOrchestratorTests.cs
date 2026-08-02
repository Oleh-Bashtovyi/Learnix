using System.Runtime.CompilerServices;
using Learnix.Application.AiChat.Abstractions;
using Learnix.Application.AiChat.Abstractions.Models;
using Learnix.Application.AiChat.Constants;
using Learnix.Application.AiChat.Services;
using Learnix.Application.AiChat.Tools;
using Learnix.Application.Common.Options;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Learnix.Application.UnitTests.AiChat.Services;

public class ChatStreamOrchestratorTests
{
    private readonly IChatSessionRepository _sessionRepository = Substitute.For<IChatSessionRepository>();
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly IAiAvailabilityStore _availability = Substitute.For<IAiAvailabilityStore>();

    public ChatStreamOrchestratorTests()
    {
        _sessionRepository
            .GetOrCreateAsync(Arg.Any<Guid>(), Arg.Any<ChatScope>(), Arg.Any<CancellationToken>())
            .Returns(new ChatSession { Id = "session-1" });
    }

    [Fact]
    public async Task Handle_WhenAToolThrows_ShouldReturnAnErrorResultInsteadOfCrashingTheStream()
    {
        // Arrange — one turn: the model calls a tool, gets an error back, and stops (no more tool use).
        var provider = new FakeAiChatProvider(
            [new ToolUseStartEvent("call-1", "broken_tool"), new ToolUseEndEvent("call-1", "broken_tool", "{}")],
            [new TextDeltaEvent("Sorry, that failed.")]);

        var brokenTool = Substitute.For<IChatTool>();
        brokenTool.Name.Returns("broken_tool");
        brokenTool.IsAvailableIn(ChatScopeType.Platform).Returns(true);
        brokenTool.Definition.Returns(new ToolDefinition("broken_tool", "desc", "{}"));
        brokenTool.ExecuteAsync(Arg.Any<ChatToolInvocation>(), Arg.Any<CancellationToken>())
            .Returns<Task<string>>(_ => throw new InvalidOperationException("boom"));

        var sut = NewSut(provider, [brokenTool]);

        // Act
        var events = await Collect(sut, ChatScope.Platform);

        // Assert
        events.Should().Contain(e => e.EventType == "message_end");

        await _sessionRepository.Received(1).AppendMessagesAsync(
            "session-1",
            Arg.Is<IEnumerable<ChatMessage>>(msgs => msgs.Any(m =>
                m.Role == "tool_result" &&
                m.ToolCalls!.Any(tc => tc.ResultJson == "{\"error\":\"Tool execution failed\"}"))),
            Arg.Any<int>(),
            Arg.Any<CancellationToken>());

        await _availability.Received(1).ReportSuccessAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenTheModelStillWantsToolsOnTheFifthTurn_ShouldForceAFinalNoToolsCallInsteadOfEndingBlank()
    {
        // Arrange — five turns that all request the same tool again, then a sixth call must happen with
        // tools withheld to force a real text answer.
        var repeatedToolUse = new List<ChatStreamEvent> { new ToolUseStartEvent("call-x", "search_courses"), new ToolUseEndEvent("call-x", "search_courses", "{}") };
        var provider = new FakeAiChatProvider(
            repeatedToolUse, repeatedToolUse, repeatedToolUse, repeatedToolUse, repeatedToolUse,
            [new TextDeltaEvent("Here is what I found.")]);

        var tool = Substitute.For<IChatTool>();
        tool.Name.Returns("search_courses");
        tool.IsAvailableIn(ChatScopeType.Platform).Returns(true);
        tool.Definition.Returns(new ToolDefinition("search_courses", "desc", "{}"));
        tool.ExecuteAsync(Arg.Any<ChatToolInvocation>(), Arg.Any<CancellationToken>())
            .Returns("{\"courses\":[]}");

        var sut = NewSut(provider, [tool]);

        // Act
        var events = await Collect(sut, ChatScope.Platform);

        // Assert
        provider.CallCount.Should().Be(6, "5 tool turns plus one forced text-only synthesis call");
        provider.ToolCountByCall[5].Should().Be(0, "tools must be withheld on the forced final call");

        events.Should().Contain(e => e.EventType == "text_delta" && e.Data.Contains("Here is what I found."));
        events.Should().Contain(e => e.EventType == "message_end");

        await _sessionRepository.Received(1).AppendMessagesAsync(
            "session-1",
            Arg.Is<IEnumerable<ChatMessage>>(msgs =>
                msgs.Any(m => m.Role == "assistant" && m.Content == "Here is what I found.")),
            Arg.Any<int>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenTheProviderStopsForMaxTokens_ShouldMarkMessageEndAsTruncated()
    {
        // Arrange — a single turn that ends because the output budget ran out, not because the model
        // was actually done. The two providers spell this differently at the MessageEndEvent boundary
        // (Anthropic: "max_tokens", Gemini's enum: "MaxTokens"); this drives the raw provider value
        // straight through FakeAiChatProvider like AnthropicChatProvider/GeminiChatProvider do.
        var provider = new FakeAiChatProvider(
            reasons: ["max_tokens"],
            [new TextDeltaEvent("This answer got cut off mid-")]);

        var sut = NewSut(provider, []);

        // Act
        var events = await Collect(sut, ChatScope.Platform);

        // Assert
        var messageEnd = events.Should().ContainSingle(e => e.EventType == "message_end").Subject;
        messageEnd.Data.Should().Contain("\"finishReason\":\"max_tokens\"");
        messageEnd.Data.Should().Contain("\"truncated\":true");
    }

    [Fact]
    public async Task Handle_WhenTheProviderFinishesNormally_ShouldNotMarkMessageEndAsTruncated()
    {
        // Arrange
        var provider = new FakeAiChatProvider(
            reasons: ["end_turn"],
            [new TextDeltaEvent("A complete answer.")]);

        var sut = NewSut(provider, []);

        // Act
        var events = await Collect(sut, ChatScope.Platform);

        // Assert
        var messageEnd = events.Should().ContainSingle(e => e.EventType == "message_end").Subject;
        messageEnd.Data.Should().Contain("\"finishReason\":\"end_turn\"");
        messageEnd.Data.Should().Contain("\"truncated\":false");
    }

    [Fact]
    public async Task Handle_WhenTheModelCallsAnUnregisteredTool_ShouldReturnANotFoundErrorInsteadOfCrashing()
    {
        // Arrange — the model asks for a tool name that isn't in this scope's tool list at all (hallucinated,
        // or valid in a different scope). ToolName travels straight from the model's own output into the
        // result JSON, so this also guards the string-building itself: a name with a quote in it must still
        // produce valid, parseable JSON rather than a string that only happens to look like it.
        var provider = new FakeAiChatProvider(
            [new ToolUseStartEvent("call-1", "ghost\"tool"), new ToolUseEndEvent("call-1", "ghost\"tool", "{}")],
            [new TextDeltaEvent("Never mind.")]);

        var sut = NewSut(provider, []);

        // Act
        var events = await Collect(sut, ChatScope.Platform);

        // Assert
        events.Should().Contain(e => e.EventType == ChatSseEventTypes.MessageEnd);

        await _sessionRepository.Received(1).AppendMessagesAsync(
            "session-1",
            Arg.Is<IEnumerable<ChatMessage>>(msgs => msgs.Any(m =>
                m.Role == ChatMessageRoles.ToolResult &&
                m.ToolCalls!.Any(tc => ErrorMessageOf(tc.ResultJson) == "Tool 'ghost\"tool' not found"))),
            Arg.Any<int>(),
            Arg.Any<CancellationToken>());
    }

    /// <summary>Parses a tool result written as <c>{"error":"..."}</c> and returns the message — throws if
    /// the JSON is malformed, which is itself part of what the caller is asserting.</summary>
    private static string? ErrorMessageOf(string? resultJson) =>
        System.Text.Json.JsonDocument.Parse(resultJson!).RootElement.GetProperty("error").GetString();

    [Fact]
    public async Task Handle_WhenAToolRuns_ShouldEmitToolUseStartAndEndPayloadsCarryingCallIdAndResultsCount()
    {
        // Arrange
        var provider = new FakeAiChatProvider(
            [new ToolUseStartEvent("call-1", "search_courses"), new ToolUseEndEvent("call-1", "search_courses", "{}")],
            [new TextDeltaEvent("Found some.")]);

        var tool = Substitute.For<IChatTool>();
        tool.Name.Returns("search_courses");
        tool.IsAvailableIn(ChatScopeType.Platform).Returns(true);
        tool.Definition.Returns(new ToolDefinition("search_courses", "desc", "{}"));
        tool.ExecuteAsync(Arg.Any<ChatToolInvocation>(), Arg.Any<CancellationToken>())
            .Returns("[{\"id\":1},{\"id\":2}]");

        var sut = NewSut(provider, [tool]);

        // Act
        var events = await Collect(sut, ChatScope.Platform);

        // Assert
        var start = events.Should().ContainSingle(e => e.EventType == ChatSseEventTypes.ToolUseStart).Subject;
        start.Data.Should().Contain("\"toolName\":\"search_courses\"").And.Contain("\"callId\":\"call-1\"");

        var end = events.Should().ContainSingle(e => e.EventType == ChatSseEventTypes.ToolUseEnd).Subject;
        end.Data.Should().Contain("\"callId\":\"call-1\"").And.Contain("\"resultsCount\":2");
    }

    [Fact]
    public async Task Handle_WhenTheProviderErrors_ShouldEmitThePublicOutageCodeAndSkipMessageEnd()
    {
        // Arrange — a rejected key, classified Unauthorized. ADR-BACK-CHAT-014: the client only ever sees
        // the narrowed public code, never the provider's own message.
        var provider = new FakeAiChatProvider(
            [new ProviderErrorEvent("401 invalid api key", AiOutageReasons.Unauthorized)]);

        var sut = NewSut(provider, []);

        // Act
        var events = await Collect(sut, ChatScope.Platform);

        // Assert
        var error = events.Should().ContainSingle(e => e.EventType == ChatSseEventTypes.Error).Subject;
        error.Data.Should().Contain($"\"code\":\"{AiOutageReasons.Unavailable}\"");
        error.Data.Should().NotContain("invalid api key");

        // A failed turn never persists or reports success — no message_end for a turn that didn't complete.
        events.Should().NotContain(e => e.EventType == ChatSseEventTypes.MessageEnd);

        await _availability.Received(1).ReportOutageAsync(
            Arg.Is<AiOutage>(o => o.Reason == AiOutageReasons.Unauthorized), Arg.Any<CancellationToken>());
    }

    private ChatStreamOrchestrator NewSut(IAiChatProvider provider, IReadOnlyList<IChatTool> tools) =>
        new(_sessionRepository, provider, tools, _mediator, _availability,
            Options.Create(new AiChatOptions()), NullLogger<ChatStreamOrchestrator>.Instance);

    private static async Task<List<SseEvent>> Collect(ChatStreamOrchestrator sut, ChatScope scope)
    {
        var events = new List<SseEvent>();
        await foreach (var evt in sut.StreamAsync(Guid.NewGuid(), scope, null, "hello", CancellationToken.None))
            events.Add(evt);
        return events;
    }

    /// <summary>Returns one queued turn of events per call, holding on the last queued turn if exceeded.</summary>
    private sealed class FakeAiChatProvider : IAiChatProvider
    {
        private readonly IReadOnlyList<ChatStreamEvent>[] _turns;
        private readonly IReadOnlyList<string> _finishReasons;

        public FakeAiChatProvider(params IReadOnlyList<ChatStreamEvent>[] turns)
            : this(reasons: [], turns)
        {
        }

        /// <param name="reasons">The MessageEndEvent reason per turn — "end_turn" once the queue runs out.</param>
        public FakeAiChatProvider(IReadOnlyList<string> reasons, params IReadOnlyList<ChatStreamEvent>[] turns)
        {
            _turns = turns;
            _finishReasons = reasons;
        }

        public int CallCount { get; private set; }
        public List<int> ToolCountByCall { get; } = [];

        public string Name => "Fake";
        public bool IsConfigured => true;

        public async IAsyncEnumerable<ChatStreamEvent> StreamChatAsync(
            ChatRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var turnIndex = Math.Min(CallCount, _turns.Length - 1);
            var reason = turnIndex < _finishReasons.Count ? _finishReasons[turnIndex] : "end_turn";
            CallCount++;
            ToolCountByCall.Add(request.Tools.Count);

            var turnEvents = _turns[turnIndex];

            foreach (var streamEvent in turnEvents)
            {
                await Task.Yield();
                yield return streamEvent;
            }

            // A real provider yields its error and stops (ADR-BACK-CHAT-014) — it never reaches the point
            // of reporting a finish reason for a turn that failed.
            if (turnEvents.All(e => e is not ProviderErrorEvent))
                yield return new MessageEndEvent(reason);
        }
    }
}
