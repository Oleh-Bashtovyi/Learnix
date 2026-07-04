import { useQuery } from '@tanstack/react-query';
import { instructorApplicationsApi } from '@/api/instructorApplications.api';
import { queryKeys } from '@/api/queryKeys';
import { useAuthStore } from '@/store/auth.store';

export function useMyApplication() {
    const user = useAuthStore((s) => s.user);
    return useQuery({
        queryKey: queryKeys.applications.mine(),
        queryFn: () => instructorApplicationsApi.getMine(),
        enabled: !!user,
        retry: (_, error) => {
            // 404 means no application submitted yet — don't retry
            const status = (error as { response?: { status?: number } })?.response?.status;
            return status !== 404;
        },
    });
}
