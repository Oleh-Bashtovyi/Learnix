import {
    useInstructorRatingDistribution,
    useInstructorRatingTrend,
    useInstructorRecentReviews,
} from '@/hooks/instructor/useInstructorAnalytics';
import { RatingDistributionChart } from '../components/RatingDistributionChart';
import { RatingTrendChart } from '../components/RatingTrendChart';
import { RecentReviewsList } from '../components/RecentReviewsList';

const RECENT_TAKE = 8;

interface ReviewsTabProps {
    /** Owned by the page, not this tab — it sits in the tab row next to the tab list, not above the
        charts, and needs to survive switching to another tab and back. */
    courseId: string;
}

export function ReviewsTab({ courseId }: ReviewsTabProps) {
    const selected = courseId || undefined;

    const distribution = useInstructorRatingDistribution(selected);
    const trend = useInstructorRatingTrend(selected);
    const recent = useInstructorRecentReviews(RECENT_TAKE, selected);

    return (
        <div className="space-y-6">
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
                showCourseTitle={!selected}
            />
        </div>
    );
}
