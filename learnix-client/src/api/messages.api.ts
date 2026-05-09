import { api } from './axios.instance';
import type { PaginatedResult } from '@/types/api.types';
import type {
    ConversationDetail,
    ConversationSummary,
    MessageItem,
    SendMessageRequest,
    StartConversationRequest,
    UnreadCount,
} from '@/types/message.types';

export const messagesApi = {
    getConversations: () =>
        api.get<ConversationSummary[]>('/messages/conversations').then((r) => r.data),

    getMessages: (conversationId: string, skip = 0, take = 20) =>
        api
            .get<PaginatedResult<MessageItem>>(
                `/messages/conversations/${conversationId}/messages`,
                {
                    params: { skip, take },
                },
            )
            .then((r) => r.data),

    startOrGet: (data: StartConversationRequest) =>
        api
            .post<ConversationDetail>('/messages/conversations/start-or-get', data)
            .then((r) => r.data),

    sendMessage: (conversationId: string, data: SendMessageRequest) =>
        api
            .post<MessageItem>(`/messages/conversations/${conversationId}/messages`, data)
            .then((r) => r.data),

    markRead: (conversationId: string) =>
        api.put<void>(`/messages/conversations/${conversationId}/read`).then((r) => r.data),

    getUnreadCount: () => api.get<UnreadCount>('/messages/unread-count').then((r) => r.data),
};
