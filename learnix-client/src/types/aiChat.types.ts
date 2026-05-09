export interface ChatMessageDto {
    role: string;
    content: string;
    sentAt: string;
}

export interface ChatSessionDto {
    sessionId: string;
    messages: ChatMessageDto[];
}

export interface LocalChatMessage {
    id: string;
    role: 'user' | 'assistant';
    content: string;
}
