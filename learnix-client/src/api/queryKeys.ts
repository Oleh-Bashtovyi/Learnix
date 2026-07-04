/**
 * Related ADRs:
 * - ADR-FRONT-API-003: React Query Structure & Defaults
 */
export const queryKeys = {
    courses: {
        all: ['courses'] as const,
        lists: () => [...queryKeys.courses.all, 'list'] as const,
        list: (filters: Record<string, unknown>) =>
            [...queryKeys.courses.lists(), filters] as const,
        featured: () => [...queryKeys.courses.all, 'featured'] as const,
        count: () => [...queryKeys.courses.all, 'count'] as const,
        details: () => [...queryKeys.courses.all, 'detail'] as const,
        detail: (id: string) => [...queryKeys.courses.details(), id] as const,
    },
    categories: {
        all: ['categories'] as const,
        lists: () => [...queryKeys.categories.all, 'list'] as const,
        adminList: () => [...queryKeys.categories.all, 'admin-list'] as const,
    },
    lessons: {
        all: ['lessons'] as const,
        content: (courseId: string, lessonId: string) =>
            [...queryKeys.lessons.all, 'content', courseId, lessonId] as const,
    },
    progress: {
        all: ['progress'] as const,
        course: (courseId: string) => [...queryKeys.progress.all, 'course', courseId] as const,
    },
    tests: {
        all: ['tests'] as const,
        lesson: (courseId: string, lessonId: string) =>
            [...queryKeys.tests.all, 'lesson', courseId, lessonId] as const,
        attempts: (courseId: string, lessonId: string) =>
            [...queryKeys.tests.all, 'attempts', courseId, lessonId] as const,
    },
    instructor: {
        myCourses: (filters: Record<string, unknown> = {}) => ['courses', 'mine', filters] as const,
        courseForEdit: (id: string) => ['courses', 'edit', id] as const,
        earnings: () => ['instructor', 'earnings'] as const,
    },
    applications: {
        mine: () => ['instructor-applications', 'mine'] as const,
    },
    users: {
        myProfile: () => ['users', 'me'] as const,
        profile: (id: string) => ['users', 'profile', id] as const,
    },
    achievements: {
        mine: () => ['achievements', 'me'] as const,
    },
    certificates: {
        mine: () => ['certificates', 'mine'] as const,
        course: (courseId: string) => ['certificates', 'course', courseId] as const,
    },
    reviews: {
        all: ['reviews'] as const,
        byCourse: (courseId: string) => ['reviews', 'course', courseId] as const,
        mine: (courseId: string) => ['reviews', 'course', courseId, 'mine'] as const,
    },
    enrollments: {
        mine: () => ['enrollments', 'mine'] as const,
    },
    messages: {
        all: ['messages'] as const,
        conversations: () => [...queryKeys.messages.all, 'conversations'] as const,
        messages: (conversationId: string) =>
            [...queryKeys.messages.all, 'messages', conversationId] as const,
        unreadCount: () => [...queryKeys.messages.all, 'unread-count'] as const,
    },
    notifications: {
        all: ['notifications'] as const,
        list: () => [...queryKeys.notifications.all, 'list'] as const,
        unreadCount: () => [...queryKeys.notifications.all, 'unread-count'] as const,
    },
    wishlist: {
        mine: () => ['wishlist', 'mine'] as const,
    },
    aiChat: {
        session: () => ['ai-chat', 'session'] as const,
    },
    admin: {
        all: ['admin'] as const,
        stats: () => ['admin', 'stats'] as const,
        usersList: () => ['admin', 'users'] as const,
        users: (filters: Record<string, unknown>) => ['admin', 'users', filters] as const,
        coursesList: () => ['admin', 'courses'] as const,
        courses: (filters: Record<string, unknown>) => ['admin', 'courses', filters] as const,
        applicationsList: () => ['admin', 'applications'] as const,
        applications: (params: Record<string, unknown>) =>
            ['admin', 'applications', params] as const,
    },
    config: {
        public: () => ['config', 'public'] as const,
    },
} as const;
