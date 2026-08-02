import { useQuery } from '@tanstack/react-query';
import { instructorAnalyticsApi } from '@/api/instructorAnalytics.api';
import { queryKeys } from '@/api/queryKeys';

/** Summary + parameter-less charts (statuses, popularity, rating distribution) in one request. */
export function useInstructorOverview() {
    return useQuery({
        queryKey: queryKeys.instructorAnalytics.overview(),
        queryFn: instructorAnalyticsApi.getOverview,
    });
}

/** Per-day enrollments and revenue over the given date range (yyyy-MM-dd, inclusive). */
export function useInstructorDynamics(startDate: string, endDate: string) {
    return useQuery({
        queryKey: queryKeys.instructorAnalytics.dynamics(startDate, endDate),
        queryFn: () => instructorAnalyticsApi.getDynamics(startDate, endDate),
        // Switching the 7/30/90-day range is a new query key, so without this the chart would
        // collapse to ChartCard's loading spinner (a fraction of the chart's height) for every
        // range the instructor hasn't already visited, and everything below it would jump up and
        // back down. Keeping the previous range's data on screen while the new one loads avoids it.
        placeholderData: (prev) => prev,
    });
}

export function useInstructorRatingDistribution(courseId?: string) {
    return useQuery({
        queryKey: queryKeys.instructorAnalytics.ratingDistribution(courseId),
        queryFn: () => instructorAnalyticsApi.getRatingDistribution(courseId),
    });
}

export function useInstructorRecentReviews(take: number, courseId?: string) {
    return useQuery({
        queryKey: queryKeys.instructorAnalytics.recentReviews(take, courseId),
        queryFn: () => instructorAnalyticsApi.getRecentReviews(take, courseId),
    });
}

export function useInstructorRatingTrend(courseId?: string) {
    return useQuery({
        queryKey: queryKeys.instructorAnalytics.ratingTrend(courseId),
        queryFn: () => instructorAnalyticsApi.getRatingTrend(courseId),
    });
}

/**
 * Per-test score/pass-rate stats for one course; only runs once a course is selected. Unlike the
 * reviews/rating queries, there is no "all courses" mode — the backend resolves lesson titles by
 * loading that course's curriculum, so leaving this unfiltered would mean loading every owned
 * course's curriculum on every visit (ADR-BACK-LMS-008).
 */
export function useInstructorTestPerformance(courseId: string | undefined) {
    return useQuery({
        queryKey: queryKeys.instructorAnalytics.testPerformance(courseId ?? ''),
        queryFn: () => instructorAnalyticsApi.getTestPerformance(courseId!),
        enabled: !!courseId,
    });
}

export function useInstructorEngagement() {
    return useQuery({
        queryKey: queryKeys.instructorAnalytics.engagement(),
        queryFn: instructorAnalyticsApi.getEngagement,
    });
}

/** Per-lesson completion for one course; only runs once a course is selected. */
export function useInstructorLessonDropOff(courseId: string | undefined) {
    return useQuery({
        queryKey: queryKeys.instructorAnalytics.lessonDropOff(courseId ?? ''),
        queryFn: () => instructorAnalyticsApi.getLessonDropOff(courseId!),
        enabled: !!courseId,
    });
}
