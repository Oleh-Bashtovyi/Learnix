import { useMutation, useQueryClient } from '@tanstack/react-query';
import { messagesApi } from '@/api/messages.api';
import { queryKeys } from '@/api/queryKeys';

/**
 * Starts (or resumes) a student's conversation with a course's instructor. Always invalidates the
 * conversations list so a freshly created thread shows up wherever it renders — the messages page
 * and the course player each pass their own onSuccess to mutate() for what they do with the result
 * (select it in the list, open the assistant panel's chat tab).
 */
export function useStartOrGetConversation() {
    const queryClient = useQueryClient();

    return useMutation({
        mutationFn: (courseId: string) => messagesApi.startOrGet({ courseId }),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: queryKeys.messages.conversations() });
        },
    });
}
