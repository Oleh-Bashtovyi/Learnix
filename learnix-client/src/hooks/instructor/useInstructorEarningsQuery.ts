import { useQuery } from '@tanstack/react-query';
import { paymentsApi } from '@/api/payments.api';
import { queryKeys } from '@/api/queryKeys';

export function useInstructorEarningsQuery() {
    return useQuery({
        queryKey: queryKeys.instructor.earnings(),
        queryFn: paymentsApi.getInstructorEarnings,
    });
}
