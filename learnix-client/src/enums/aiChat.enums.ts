/**
 * The values a stored chat turn's role takes. Mirrors
 * Learnix.Application.AiChat.Constants.ChatMessageRoles on the backend — `ToolResult` is part of
 * the stored session but never reaches the UI: `useAiChat` filters session history down to
 * `User`/`Assistant` before it becomes a `LocalChatMessage`.
 */
export const ChatMessageRole = {
    User: 'user',
    Assistant: 'assistant',
    ToolResult: 'tool_result',
} as const;
export type ChatMessageRole = (typeof ChatMessageRole)[keyof typeof ChatMessageRole];

/**
 * The `event:` names the backend puts on the AI chat SSE stream. Mirrors
 * Learnix.Application.AiChat.Constants.ChatSseEventTypes — different language on each side, so a
 * rename on the backend has to be kept in sync here by hand.
 */
export const ChatSseEventType = {
    TextDelta: 'text_delta',
    ToolUseStart: 'tool_use_start',
    ToolUseEnd: 'tool_use_end',
    MessageEnd: 'message_end',
    Error: 'error',
} as const;
export type ChatSseEventType = (typeof ChatSseEventType)[keyof typeof ChatSseEventType];
