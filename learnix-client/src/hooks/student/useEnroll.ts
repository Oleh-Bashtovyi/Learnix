import { useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { enrollmentsApi } from '@/api/enrollments.api';
import { queryKeys } from '@/api/queryKeys';

/**
 * Related ADRs:
 * - ADR-FRONT-API-007: Data Fetching Abstraction (Custom Hooks)
 */
export function useEnroll() {
    const queryClient = useQueryClient();

    return useMutation({
        mutationFn: (courseId: string) => enrollmentsApi.enroll(courseId),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: queryKeys.enrollments.mine() });
            toast.success('Successfully enrolled!');
        },
    });
}
