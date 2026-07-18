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

export function useInstructorTestPerformance() {
    return useQuery({
        queryKey: queryKeys.instructorAnalytics.testPerformance(),
        queryFn: instructorAnalyticsApi.getTestPerformance,
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
