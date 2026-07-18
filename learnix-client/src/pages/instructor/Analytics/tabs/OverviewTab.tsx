import { useInstructorOverview } from '@/hooks/instructor/useInstructorAnalytics';
import { CourseStatusesChart } from '../components/CourseStatusesChart';
import { DynamicsChart } from '../components/DynamicsChart';
import { PopularityChart } from '../components/PopularityChart';
import { StatCards } from '../components/StatCards';

export function OverviewTab() {
    const { data: overview, isLoading, isError, refetch } = useInstructorOverview();

    return (
        <div className="space-y-6">
            <StatCards summary={overview?.summary} isLoading={isLoading} />

            <DynamicsChart />

            <div className="grid items-start gap-6 lg:grid-cols-3">
                <PopularityChart
                    className="lg:col-span-2"
                    data={overview?.popularity}
                    isLoading={isLoading}
                    isError={isError}
                    onRetry={() => refetch()}
                />
                <CourseStatusesChart
                    statuses={overview?.courseStatuses}
                    isLoading={isLoading}
                    isError={isError}
                    onRetry={() => refetch()}
                />
            </div>
        </div>
    );
}
