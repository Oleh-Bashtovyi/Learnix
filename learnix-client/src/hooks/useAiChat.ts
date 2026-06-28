import { useCallback, useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { aiChatApi, streamAiMessage } from '@/api/aiChat.api';
import { queryKeys } from '@/api/queryKeys';
import type { LocalChatMessage } from '@/types/aiChat.types';

let msgCounter = 0;
const nextId = () => `msg-${Date.now()}-${++msgCounter}`;

export function useAiChat(isOpen: boolean) {
    const { t } = useTranslation('aiChat');
    const [messages, setMessages] = useState<LocalChatMessage[]>([]);
    const [streamingContent, setStreamingContent] = useState('');
    const [isStreaming, setIsStreaming] = useState(false);
    const [activeToolName, setActiveToolName] = useState<string | null>(null);
    const [sessionLoaded, setSessionLoaded] = useState(false);
    const streamingRef = useRef('');
    const abortRef = useRef<AbortController | null>(null);
    const queryClient = useQueryClient();

    const { data: session, isLoading: isSessionLoading } = useQuery({
        queryKey: queryKeys.aiChat.session(),
        queryFn: aiChatApi.getSession,
        enabled: isOpen && !sessionLoaded,
        staleTime: Infinity,
    });

    if (session && !sessionLoaded) {
        setSessionLoaded(true);
        const localMsgs: LocalChatMessage[] = session.messages
            .filter((m) => m.role === 'user' || m.role === 'assistant')
            .map((m) => ({
                id: nextId(),
                role: m.role as 'user' | 'assistant',
                content: m.content,
            }));
        setMessages(localMsgs);
    }

    const { mutate: clearSession, isPending: isClearing } = useMutation({
        mutationFn: aiChatApi.clearSession,
        onSuccess: () => {
            abortRef.current?.abort();
            streamingRef.current = '';
            setMessages([]);
            setStreamingContent('');
            setIsStreaming(false);
            setActiveToolName(null);
            setSessionLoaded(false);
            queryClient.removeQueries({ queryKey: queryKeys.aiChat.session() });
        },
        onError: () => toast.error(t('error')),
    });

    const sendMessage = useCallback(
        async (text: string) => {
            if (isStreaming || !text.trim()) return;

            abortRef.current?.abort();
            const controller = new AbortController();
            abortRef.current = controller;

            setMessages((prev) => [...prev, { id: nextId(), role: 'user', content: text }]);
            setIsStreaming(true);
            streamingRef.current = '';
            setStreamingContent('');

            try {
                for await (const event of streamAiMessage(text, controller.signal)) {
                    if (controller.signal.aborted) break;

                    if (event.type === 'text_delta') {
                        const delta = (event.data as { content: string }).content ?? '';
                        streamingRef.current += delta;
                        setStreamingContent(streamingRef.current);
                    } else if (event.type === 'tool_use_start') {
                        const { toolName } = event.data as { toolName: string; callId: string };
                        setActiveToolName(toolName);
                    } else if (event.type === 'tool_use_end') {
                        setActiveToolName(null);
                    } else if (event.type === 'message_end') {
                        const finalContent = streamingRef.current;
                        streamingRef.current = '';
                        setStreamingContent('');
                        if (finalContent) {
                            setMessages((prev) => [
                                ...prev,
                                { id: nextId(), role: 'assistant', content: finalContent },
                            ]);
                        }
                        setIsStreaming(false);
                        break;
                    } else if (event.type === 'error') {
                        toast.error(t('error'));
                        streamingRef.current = '';
                        setStreamingContent('');
                        setIsStreaming(false);
                        setActiveToolName(null);
                        break;
                    }
                }
            } catch {
                if (!controller.signal.aborted) {
                    toast.error(t('error'));
                }
                streamingRef.current = '';
                setStreamingContent('');
                setIsStreaming(false);
                setActiveToolName(null);
            }
        },
        [isStreaming, t],
    );

    return {
        messages,
        streamingContent,
        isStreaming,
        activeToolName,
        isSessionLoading,
        sendMessage,
        clearSession,
        isClearing,
    };
}
