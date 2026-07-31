import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Activity } from 'lucide-react';
import { StatTile } from '@/components/common/elements/StatTile';
import { StatValueSkeleton } from '@/components/common/elements/StatValueSkeleton';
import {
    useInstructorEngagement,
    useInstructorLessonDropOff,
} from '@/hooks/instructor/useInstructorAnalytics';
import { useMyCoursesQuery } from '@/hooks/instructor/useMyCoursesQuery';
import { DropOffChart } from '../components/DropOffChart';
import { FunnelChart } from '../components/FunnelChart';

export function EngagementTab() {
    const { t } = useTranslation('instructorAnalytics');
    const engagement = useInstructorEngagement();

    const { data: coursesData } = useMyCoursesQuery({ take: 100 });
    const courses = coursesData?.items ?? [];

    // The drop-off curve is per-course; default to the first course so the chart is never empty on open.
    const [picked, setPicked] = useState('');
    const selectedCourseId = picked || courses[0]?.id || '';
    const dropOff = useInstructorLessonDropOff(selectedCourseId || undefined);

    return (
        <div className="space-y-6">
            <div className="grid items-start gap-6 lg:grid-cols-2">
                <FunnelChart
                    data={engagement.data}
                    isLoading={engagement.isLoading}
                    isError={engagement.isError}
                    onRetry={() => engagement.refetch()}
                />

                <StatTile
                    icon={<Activity className="size-5" />}
                    tone="accent"
                    surface="card"
                    label={t('engagement.activeStudents')}
                    hint={t('engagement.activeStudentsHint')}
                    value={
                        engagement.isLoading ? (
                            <StatValueSkeleton />
                        ) : engagement.isError ? (
                            '—'
                        ) : (
                            (engagement.data?.activeStudentsLast30Days ?? 0).toLocaleString()
                        )
                    }
                />
            </div>

            <DropOffChart
                courses={courses}
                courseId={selectedCourseId}
                onCourseChange={setPicked}
                data={dropOff.data}
                isLoading={dropOff.isLoading}
                isError={dropOff.isError}
                onRetry={() => dropOff.refetch()}
            />
        </div>
    );
}
