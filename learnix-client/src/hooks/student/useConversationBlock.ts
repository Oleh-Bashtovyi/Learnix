import { useMutation, useQueryClient } from '@tanstack/react-query';
import { messagesApi } from '@/api/messages.api';
import { queryKeys } from '@/api/queryKeys';

function useConversationBlockMutation(mutationFn: (conversationId: string) => Promise<void>) {
    const queryClient = useQueryClient();

    return useMutation({
        mutationFn,
        onSuccess: (_data, conversationId) => {
            queryClient.invalidateQueries({ queryKey: queryKeys.messages.conversations() });
            queryClient.invalidateQueries({
                queryKey: queryKeys.messages.messages(conversationId),
            });
        },
    });
}

export function useBlockConversation() {
    return useConversationBlockMutation(messagesApi.block);
}

export function useUnblockConversation() {
    return useConversationBlockMutation(messagesApi.unblock);
}
