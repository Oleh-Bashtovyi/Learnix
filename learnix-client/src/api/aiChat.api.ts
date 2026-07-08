import type { ChatSessionDto } from '@/types/aiChat.types';
import { api } from './axios.instance';

export const aiChatApi = {
    getSession: () => api.get<ChatSessionDto>('/ai-chat/session').then((r) => r.data),

    clearSession: () => api.delete('/ai-chat/session').then((r) => r.data),
};

export async function* streamAiMessage(
    message: string,
    signal?: AbortSignal,
): AsyncGenerator<{ type: string; data: unknown }> {
    const response = await api.post(
        '/ai-chat/messages',
        { message },
        {
            responseType: 'stream',
            adapter: 'fetch', // Use fetch adapter to support streaming in the browser
            signal,
        },
    );

    const stream = response.data as unknown as ReadableStream<Uint8Array>;
    if (!stream) {
        throw new Error('No stream in response');
    }

    const reader = stream.getReader();
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
