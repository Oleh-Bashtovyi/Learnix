import { useQuery } from '@tanstack/react-query';
import { enrollmentsApi } from '@/api/enrollments.api';
import { queryKeys } from '@/api/queryKeys';
import { useAuthStore } from '@/store/auth.store';

export function useMyEnrollments() {
    const user = useAuthStore((s) => s.user);

    return useQuery({
        queryKey: queryKeys.enrollments.mine(),
        queryFn: () => enrollmentsApi.getMyEnrollments(),
        enabled: !!user,
    });
}
