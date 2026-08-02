import { useInstructorTestPerformance } from '@/hooks/instructor/useInstructorAnalytics';
import { TestPerformanceTable } from '../components/TestPerformanceTable';

interface TestsTabProps {
    /** Owned by the page, not this tab — see the comment on the tab row in InstructorAnalyticsPage. */
    courseId: string;
}

export function TestsTab({ courseId }: TestsTabProps) {
    const { data, isLoading, isError, refetch } = useInstructorTestPerformance(
        courseId || undefined,
    );

    return (
        <TestPerformanceTable
            data={data}
            isLoading={isLoading}
            isError={isError}
            onRetry={() => refetch()}
        />
    );
}
