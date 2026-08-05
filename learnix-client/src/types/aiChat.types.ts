import { ChatMessageRole } from '@/enums/aiChat.enums';

export interface ChatMessageDto {
    role: ChatMessageRole;
    content: string;
    sentAt: string;
}

export interface ChatSessionDto {
    sessionId: string;
    messages: ChatMessageDto[];
}

/**
 * Why the assistant cannot answer, as far as a student is told. The backend knows more — a rejected or a
 * missing API key — but reports those as `unavailable`: they are the operator's problem, and the detail
 * belongs in the logs, not on the wire (ADR-CHAT-014).
 */
export type AiOutageReason = 'quota_exceeded' | 'unavailable';

/**
 * Whether the assistant can answer right now. The backend learns this from real chat turns rather than
 * from health-check pings — on a free tier a ping would spend the very quota it checks (ADR-CHAT-014).
 */
export interface AiChatStatusDto {
    available: boolean;
    provider: string;
    reason: AiOutageReason | null;
    /** When the provider is expected back, when it says so. */
    retryAtUtc: string | null;
}

export interface LocalChatMessage {
    id: string;
    role: Extract<ChatMessageRole, 'user' | 'assistant'>;
    content: string;
    /** True when the provider stopped for running out of output budget, not because it was done. */
    truncated?: boolean;
}

/**
 * Which conversation this is. The platform assistant and each course tutor keep separate
 * histories, budgets and tools — clearing one leaves the others untouched.
 */
export type ChatScope = { kind: 'platform' } | { kind: 'course'; courseId: string };
