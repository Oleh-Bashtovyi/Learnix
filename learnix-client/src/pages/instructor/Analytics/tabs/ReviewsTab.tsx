import { useState } from 'react';
import {
    useInstructorRatingDistribution,
    useInstructorRatingTrend,
    useInstructorRecentReviews,
} from '@/hooks/instructor/useInstructorAnalytics';
import { useMyCoursesQuery } from '@/hooks/instructor/useMyCoursesQuery';
import { CourseFilter } from '../components/CourseFilter';
import { RatingDistributionChart } from '../components/RatingDistributionChart';
import { RatingTrendChart } from '../components/RatingTrendChart';
import { RecentReviewsList } from '../components/RecentReviewsList';

const RECENT_TAKE = 8;

export function ReviewsTab() {
    const [courseId, setCourseId] = useState('');
    const selected = courseId || undefined;

    const { data: coursesData } = useMyCoursesQuery({ take: 100 });
    const courses = coursesData?.items ?? [];

    const distribution = useInstructorRatingDistribution(selected);
    const trend = useInstructorRatingTrend(selected);
    const recent = useInstructorRecentReviews(RECENT_TAKE, selected);

    return (
        <div className="space-y-6">
            <div className="flex justify-end">
                <CourseFilter courses={courses} value={courseId} onChange={setCourseId} />
            </div>

            <div className="grid items-start gap-6 lg:grid-cols-2">
                <RatingDistributionChart
                    distribution={distribution.data}
                    isLoading={distribution.isLoading}
                    isError={distribution.isError}
                    onRetry={() => distribution.refetch()}
                />
                <RatingTrendChart
                    data={trend.data}
                    isLoading={trend.isLoading}
                    isError={trend.isError}
                    onRetry={() => trend.refetch()}
                />
            </div>

            <RecentReviewsList
                data={recent.data}
                isLoading={recent.isLoading}
                isError={recent.isError}
                onRetry={() => recent.refetch()}
            />
        </div>
    );
}
