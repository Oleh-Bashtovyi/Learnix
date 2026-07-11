using System.Text.Json;
using Learnix.Application.AiChat.Abstractions.Models;
using Learnix.Application.AiChat.Constants;

namespace Learnix.Application.AiChat.Services;

/// <summary>
/// Drops the bodies of lesson-bound tool results that no longer describe the lesson the student has open
/// (ADR-CHAT-013).
/// <para>
/// A tool result is persisted and replayed inside the sliding window on every later turn (ADR-CHAT-005).
/// Without this, walking through a course drags every lesson body the student ever opened along with it —
/// and coming back to an earlier lesson has the model fetch its body a second time, so the same 8 000
/// characters sit in the window twice. Here the window keeps exactly one live body per lesson-bound tool:
/// the newest one that is about the current lesson. Everything else is replaced by a short note.
/// </para>
/// <para>
/// The messages themselves are never removed: both providers reject a <c>tool_result</c> whose
/// <c>tool_use</c> is missing. Only the payload inside them is swapped, and only for the request — the
/// stored session keeps the full result, so returning to a lesson revives its body instead of re-fetching it.
/// </para>
/// </summary>
public static class ChatToolResultCompactor
{
    private const string ToolResultRole = "tool_result";
    private const string LessonIdProperty = "lessonId";

    private static readonly HashSet<string> LessonBoundTools =
    [
        ChatToolNames.GetCurrentLesson,
        ChatToolNames.GetMyTestReview
    ];

    private static readonly string SupersededResult = JsonSerializer.Serialize(new
    {
        note = "Superseded. This result described a lesson the student no longer has open. The lesson they "
            + "have open now is the one in <current_lesson>; call the tool again if you need its content."
    });

    /// <param name="currentLessonId">The lesson the student has open, or null when they have none.</param>
    public static IReadOnlyList<ChatMessage> Compact(
        IReadOnlyList<ChatMessage> window,
        Guid? currentLessonId)
    {
        var survivors = FindLiveResults(window, currentLessonId);

        return window
            .Select((message, index) => CompactMessage(message, index, survivors))
            .ToList();
    }

    /// <summary>
    /// The newest result of each lesson-bound tool that is about the current lesson, as (message, call) pairs.
    /// Nothing survives when the student has no lesson open — every stored body is then about some other one.
    /// </summary>
    private static HashSet<(int Message, int Call)> FindLiveResults(
        IReadOnlyList<ChatMessage> window,
        Guid? currentLessonId)
    {
        var live = new Dictionary<string, (int Message, int Call)>();

        if (currentLessonId is null)
            return [];

        for (var i = 0; i < window.Count; i++)
        {
            if (window[i].Role != ToolResultRole || window[i].ToolCalls is not { } calls)
                continue;

            for (var j = 0; j < calls.Count; j++)
            {
                if (!LessonBoundTools.Contains(calls[j].ToolName))
                    continue;

                // Later results win: a re-fetch of the same lesson collapses onto its newest copy.
                if (TryReadLessonId(calls[j].ResultJson) == currentLessonId)
                    live[calls[j].ToolName] = (i, j);
            }
        }

        return live.Values.ToHashSet();
    }

    private static ChatMessage CompactMessage(
        ChatMessage message,
        int index,
        HashSet<(int Message, int Call)> survivors)
    {
        if (message.Role != ToolResultRole || message.ToolCalls is not { } calls)
            return message;

        var compacted = calls
            .Select((call, j) => LessonBoundTools.Contains(call.ToolName) && !survivors.Contains((index, j))
                ? call with { ResultJson = SupersededResult }
                : call)
            .ToList();

        return compacted.SequenceEqual(calls) ? message : message with { ToolCalls = compacted };
    }

    /// <summary>
    /// The lesson a stored result is about. Every lesson-bound tool nests its payload under a single
    /// property (<c>currentLesson</c>, <c>testReview</c>) carrying a <c>lessonId</c>; an error result carries
    /// none, and is therefore never a survivor.
    /// </summary>
    private static Guid? TryReadLessonId(string? resultJson)
    {
        if (string.IsNullOrEmpty(resultJson))
            return null;

        try
        {
            using var document = JsonDocument.Parse(resultJson);

            if (document.RootElement.ValueKind != JsonValueKind.Object)
                return null;

            foreach (var propertyValue in document.RootElement.EnumerateObject().Select(property => property.Value))
            {
                if (propertyValue.ValueKind == JsonValueKind.Object
                    && propertyValue.TryGetProperty(LessonIdProperty, out var lessonId)
                    && lessonId.TryGetGuid(out var value))
                {
                    return value;
                }
            }
        }
        catch (JsonException)
        {
            // A result we cannot read is a result we cannot vouch for: treat it as superseded.
        }

        return null;
    }
}
