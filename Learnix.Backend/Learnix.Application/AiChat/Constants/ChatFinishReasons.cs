namespace Learnix.Application.AiChat.Constants;

/// <summary>
/// The default finish reason assumed when a provider ends a turn without saying why. Anthropic's own
/// vocabulary for a normal stop is <c>end_turn</c>, used by its provider when the stream ends with no
/// stop reason on the final chunk; the orchestrator falls back to the same value when no turn reported
/// one at all, since that path is only reachable on an otherwise-successful stream.
/// </summary>
public static class ChatFinishReasons
{
    public const string EndTurn = "end_turn";
}
