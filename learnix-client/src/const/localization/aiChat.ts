export const AI_CHAT = {
    TITLE: 'AI Assistant',
    PLACEHOLDER: 'Ask anything...',
    SEND: 'Send',
    CLEAR: 'New conversation',
    SEARCHING: 'Searching courses...',
    THINKING: 'Thinking...',
    LOOKING_UP_INFO: 'Looking up info...',
    ERROR: 'Something went wrong. Please try again.',
    WELCOME:
        "Hi! I'm your Learnix AI assistant. I can help you find courses, answer questions about learning, and more.",
    ARIA_TOGGLE: 'Toggle AI chat',
    ARIA_CLOSE: 'Close chat',
    ARIA_CLEAR: 'Start new conversation',
} as const;

export function getToolLabel(toolName: string): string {
    switch (toolName) {
        case 'search_courses':
        case 'get_categories':
            return AI_CHAT.SEARCHING;
        case 'get_platform_info':
            return AI_CHAT.LOOKING_UP_INFO;
        default:
            return AI_CHAT.THINKING;
    }
}
