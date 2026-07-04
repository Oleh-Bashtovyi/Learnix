namespace Learnix.Application.AiChat.Queries.GetActiveChatSession;

public sealed record ChatSessionDto(
    string SessionId,
    IReadOnlyList<ChatMessageDto> Messages);

public sealed record ChatMessageDto(
    string Role,
    string Content,
    DateTime SentAt);
