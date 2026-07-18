import type {
    InstructorDynamicsItem,
    InstructorEngagement,
    InstructorOverview,
    InstructorRatingTrendItem,
    InstructorRecentReview,
    InstructorTestPerformanceItem,
    LessonDropOff,
    RatingDistribution,
} from '@/types/instructorAnalytics.types';
import { api } from './axios.instance';

export const instructorAnalyticsApi = {
    getOverview: () =>
        api.get<InstructorOverview>('/instructor/analytics/overview').then((r) => r.data),

    getDynamics: (startDate: string, endDate: string) =>
        api
            .get<InstructorDynamicsItem[]>('/instructor/analytics/dynamics', {
                params: { startDate, endDate },
            })
            .then((r) => r.data),

    getRatingDistribution: (courseId?: string) =>
        api
            .get<RatingDistribution>('/instructor/analytics/reviews/distribution', {
                params: courseId ? { courseId } : undefined,
            })
            .then((r) => r.data),

    getRecentReviews: (take: number, courseId?: string) =>
        api
            .get<InstructorRecentReview[]>('/instructor/analytics/reviews/recent', {
                params: { take, ...(courseId ? { courseId } : {}) },
            })
            .then((r) => r.data),

    getRatingTrend: (courseId?: string) =>
        api
            .get<InstructorRatingTrendItem[]>('/instructor/analytics/reviews/trend', {
                params: courseId ? { courseId } : undefined,
            })
            .then((r) => r.data),

    getTestPerformance: () =>
        api
            .get<InstructorTestPerformanceItem[]>('/instructor/analytics/tests/performance')
            .then((r) => r.data),

    getEngagement: () =>
        api.get<InstructorEngagement>('/instructor/analytics/engagement').then((r) => r.data),

    getLessonDropOff: (courseId: string) =>
        api
            .get<LessonDropOff>('/instructor/analytics/engagement/drop-off', {
                params: { courseId },
            })
            .then((r) => r.data),
};
