import { useCallback, useEffect, useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { aiChatApi, streamAiMessage } from '@/api/aiChat.api';
import { queryKeys } from '@/api/queryKeys';
import { ChatMessageRole, ChatSseEventType } from '@/enums/aiChat.enums';
import type { AiOutageReason, ChatScope, LocalChatMessage } from '@/types/aiChat.types';

let msgCounter = 0;
const nextId = () => `msg-${Date.now()}-${++msgCounter}`;

export type AiChatController = ReturnType<typeof useAiChat>;

const keyOf = (scope: ChatScope) =>
    scope.kind === 'platform' ? 'platform' : `course:${scope.courseId}`;

/**
 * Owns one AI chat session. Call it from the component that outlives the chat surface,
 * so an in-flight stream and the message list survive the panel being closed.
 *
 * @param scope which conversation — the platform assistant or a course tutor.
 *   Pass a stable reference (a module constant or `useMemo`); it feeds a query key and a callback.
 * @param lessonId the lesson the student has open, sent alongside each message of a course tutor.
 */
export function useAiChat(isOpen: boolean, scope: ChatScope, lessonId?: string) {
    const { t } = useTranslation('aiChat');
    const [messages, setMessages] = useState<LocalChatMessage[]>([]);
    const [streamingContent, setStreamingContent] = useState('');
    const [isStreaming, setIsStreaming] = useState(false);
    const [activeToolName, setActiveToolName] = useState<string | null>(null);
    const [sessionLoaded, setSessionLoaded] = useState(false);
    const streamingRef = useRef('');
    const abortRef = useRef<AbortController | null>(null);
    const queryClient = useQueryClient();

    const scopeKey = keyOf(scope);
    const [prevScopeKey, setPrevScopeKey] = useState(scopeKey);

    // The player keeps this hook mounted across courses, so the scope can change underneath it.
    // Another course is another conversation: drop what belongs to the previous one.
    if (scopeKey !== prevScopeKey) {
        setPrevScopeKey(scopeKey);
        setMessages([]);
        setStreamingContent('');
        setIsStreaming(false);
        setActiveToolName(null);
        setSessionLoaded(false);
    }

    // Leaving a scope (or the page) must not leave a stream writing into the next one.
    useEffect(
        () => () => {
            abortRef.current?.abort();
            streamingRef.current = '';
        },
        [scopeKey],
    );

    const { data: session, isLoading: isSessionLoading } = useQuery({
        queryKey: queryKeys.aiChat.session(scope),
        queryFn: () => aiChatApi.getSession(scope),
        enabled: isOpen && !sessionLoaded,
        staleTime: Infinity,
    });

    // Whether the provider can answer at all — quota, key, outage. Shared by every scope, so it is not
    // keyed by one. While an outage is in force we poll: it ends by expiry, with nothing to notify us.
    const { data: status } = useQuery({
        queryKey: queryKeys.aiChat.status,
        queryFn: aiChatApi.getStatus,
        enabled: isOpen,
        staleTime: 30_000,
        refetchInterval: (query) => (query.state.data?.available === false ? 60_000 : false),
    });

    const isAiAvailable = status?.available ?? true;

    const refreshStatus = useCallback(
        () => queryClient.invalidateQueries({ queryKey: queryKeys.aiChat.status }),
        [queryClient],
    );

    if (session && !sessionLoaded) {
        setSessionLoaded(true);
        const localMsgs: LocalChatMessage[] = session.messages
            .filter((m) => m.role === ChatMessageRole.User || m.role === ChatMessageRole.Assistant)
            .map((m) => ({
                id: nextId(),
                role: m.role as LocalChatMessage['role'],
                content: m.content,
            }));
        setMessages(localMsgs);
    }

    const { mutate: clearSession, isPending: isClearing } = useMutation({
        mutationFn: () => aiChatApi.clearSession(scope),
        onSuccess: () => {
            abortRef.current?.abort();
            streamingRef.current = '';
            setMessages([]);
            setStreamingContent('');
            setIsStreaming(false);
            setActiveToolName(null);
            setSessionLoaded(false);
            queryClient.removeQueries({ queryKey: queryKeys.aiChat.session(scope) });
        },
        onError: () => toast.error(t('error')),
    });

    const sendMessage = useCallback(
        async (text: string) => {
            if (isStreaming || !text.trim() || !isAiAvailable) return;

            abortRef.current?.abort();
            const controller = new AbortController();
            abortRef.current = controller;

            setMessages((prev) => [
                ...prev,
                { id: nextId(), role: ChatMessageRole.User, content: text },
            ]);
            setIsStreaming(true);
            streamingRef.current = '';
            setStreamingContent('');

            let settled = false;

            try {
                for await (const event of streamAiMessage(
                    scope,
                    text,
                    lessonId,
                    controller.signal,
                )) {
                    if (controller.signal.aborted) break;

                    if (event.type === ChatSseEventType.TextDelta) {
                        const delta = (event.data as { content: string }).content ?? '';
                        streamingRef.current += delta;
                        setStreamingContent(streamingRef.current);
                    } else if (event.type === ChatSseEventType.ToolUseStart) {
                        const { toolName } = event.data as { toolName: string; callId: string };
                        setActiveToolName(toolName);
                    } else if (event.type === ChatSseEventType.ToolUseEnd) {
                        setActiveToolName(null);
                    } else if (event.type === ChatSseEventType.MessageEnd) {
                        settled = true;
                        const finalContent = streamingRef.current;
                        const { truncated } = event.data as { truncated?: boolean };
                        streamingRef.current = '';
                        setStreamingContent('');
                        if (finalContent) {
                            setMessages((prev) => [
                                ...prev,
                                {
                                    id: nextId(),
                                    role: ChatMessageRole.Assistant,
                                    content: finalContent,
                                    truncated,
                                },
                            ]);
                        }
                        setIsStreaming(false);
                        break;
                    } else if (event.type === ChatSseEventType.Error) {
                        settled = true;
                        // The turn just found out the provider is down — the status query says how.
                        const { code } = event.data as { code?: AiOutageReason };
                        void refreshStatus();
                        toast.error(code ? t(`unavailable.${code}`) : t('error'));
                        streamingRef.current = '';
                        setStreamingContent('');
                        setIsStreaming(false);
                        setActiveToolName(null);
                        break;
                    }
                }

                // A provider that dies mid-stream closes the connection after the SSE headers are out, so
                // neither message_end nor error ever arrives. Without this the composer stays disabled.
                if (!settled && !controller.signal.aborted) {
                    toast.error(t('error'));
                    streamingRef.current = '';
                    setStreamingContent('');
                    setIsStreaming(false);
                    setActiveToolName(null);
                }
            } catch {
                if (!controller.signal.aborted) {
                    // Includes the 503 the API answers with when it already knows the provider is out:
                    // the refetched status turns the composer off instead of letting them try again.
                    void refreshStatus();
                    toast.error(t('error'));
                }
                streamingRef.current = '';
                setStreamingContent('');
                setIsStreaming(false);
                setActiveToolName(null);
            }
        },
        [isStreaming, t, scope, lessonId, isAiAvailable, refreshStatus],
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
        status,
        isAiAvailable,
    };
}
