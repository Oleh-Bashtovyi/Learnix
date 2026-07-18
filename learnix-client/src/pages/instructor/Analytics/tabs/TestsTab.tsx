import { useInstructorTestPerformance } from '@/hooks/instructor/useInstructorAnalytics';
import { TestPerformanceTable } from '../components/TestPerformanceTable';

export function TestsTab() {
    const { data, isLoading, isError, refetch } = useInstructorTestPerformance();

    return (
        <TestPerformanceTable
            data={data}
            isLoading={isLoading}
            isError={isError}
            onRetry={() => refetch()}
        />
    );
}
