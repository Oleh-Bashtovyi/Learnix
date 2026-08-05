import { useMutation, useQueryClient } from '@tanstack/react-query';
import { paymentsApi } from '@/api/payments.api';
import { queryKeys } from '@/api/queryKeys';

/**
 * Mock checkout (no real payment gateway) — always succeeds and enrolls the student. Invalidates
 * their enrollments so My Learning picks it up immediately; the caller passes its own onSuccess
 * for what happens next.
 */
export function useInitiatePayment() {
    const queryClient = useQueryClient();
    return useMutation({
        mutationFn: (courseId: string) => paymentsApi.initiatePayment(courseId),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: queryKeys.enrollments.mine() });
        },
    });
}
