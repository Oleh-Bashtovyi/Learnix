import { api } from './axios.instance';
import { useAuthStore } from '@/store/auth.store';
import { env } from '@/utils/env';
import type { ChatSessionDto } from '@/types/aiChat.types';

export const aiChatApi = {
    getSession: () => api.get<ChatSessionDto>('/ai-chat/session').then((r) => r.data),

    clearSession: () => api.delete('/ai-chat/session').then((r) => r.data),
};

async function getValidToken(): Promise<string | null> {
    const token = useAuthStore.getState().accessToken;
    if (token) return token;

    try {
        const { data } = await api.post<{ accessToken: string }>('/auth/refresh');
        useAuthStore.getState().setAccessToken(data.accessToken);
        return data.accessToken;
    } catch {
        return null;
    }
}

export async function* streamAiMessage(
    message: string,
    signal?: AbortSignal,
): AsyncGenerator<{ type: string; data: unknown }> {
    const token = await getValidToken();

    const response = await fetch(`${env.API_URL}/ai-chat/messages`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            ...(token ? { Authorization: `Bearer ${token}` } : {}),
        },
        body: JSON.stringify({ message }),
        signal,
    });

    if (!response.ok || !response.body) {
        throw new Error(`HTTP ${response.status}`);
    }

    const reader = response.body.getReader();
    const decoder = new TextDecoder();
    let buffer = '';

    try {
        while (true) {
            const { done, value } = await reader.read();
            if (done) break;

            buffer += decoder.decode(value, { stream: true });

            // SSE events are separated by \n\n
            const blocks = buffer.split('\n\n');
            buffer = blocks.pop() ?? '';

            for (const block of blocks) {
                if (!block.trim()) continue;

                const lines = block.split('\n');
                let eventType = 'message';
                let data = '';

                for (const line of lines) {
                    if (line.startsWith('event: ')) eventType = line.slice(7).trim();
                    else if (line.startsWith('data: ')) data = line.slice(6);
                }

                if (data) {
                    try {
                        yield { type: eventType, data: JSON.parse(data) };
                    } catch {
                        yield { type: eventType, data };
                    }
                }
            }
        }
    } finally {
        reader.releaseLock();
    }
}
