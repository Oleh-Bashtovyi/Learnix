/** What a fixed recent window added, and how it compares with the window before it. */
export interface InstructorAnalyticsTrend {
    current: number;
    /** Null when the earlier window was empty — there is no change to report from nothing. */
    changePercent: number | null;
}

export interface InstructorAnalyticsSummary {
    totalStudents: number;
    totalRevenue: number;
    averageRating: number;
    certificatesIssued: number;
    newStudentsTrend: InstructorAnalyticsTrend;
    revenueTrend: InstructorAnalyticsTrend;
    certificatesTrend: InstructorAnalyticsTrend;
}

export interface CourseStatuses {
    draft: number;
    published: number;
    archived: number;
}

export interface CoursePopularityItem {
    courseId: string;
    title: string;
    enrollments: number;
}

export interface RatingDistribution {
    oneStar: number;
    twoStar: number;
    threeStar: number;
    fourStar: number;
    fiveStar: number;
}

/** The dashboard's first-paint payload — one request for the summary and the parameter-less charts. */
export interface InstructorOverview {
    summary: InstructorAnalyticsSummary;
    courseStatuses: CourseStatuses;
    popularity: CoursePopularityItem[];
    ratingDistribution: RatingDistribution;
}

export interface InstructorDynamicsItem {
    /** yyyy-MM-dd */
    date: string;
    enrollments: number;
    earnings: number;
}

export interface InstructorRecentReview {
    courseId: string;
    courseTitle: string;
    studentName: string;
    rating: number;
    text: string | null;
    createdAt: string;
}

export interface InstructorTestPerformanceItem {
    courseId: string;
    courseTitle: string;
    lessonId: string;
    lessonTitle: string;
    averageScore: number;
    maxScore: number;
    passRate: number;
}

export interface InstructorRatingTrendItem {
    /** yyyy-MM */
    month: string;
    averageRating: number;
    reviewCount: number;
}

export interface InstructorEngagement {
    enrolled: number;
    started: number;
    completed: number;
    certified: number;
    activeStudentsLast30Days: number;
}

export interface LessonDropOffItem {
    lessonId: string;
    lessonTitle: string;
    completed: number;
}

export interface LessonDropOff {
    /** Shared denominator for every lesson's completion rate. */
    enrolled: number;
    lessons: LessonDropOffItem[];
}
