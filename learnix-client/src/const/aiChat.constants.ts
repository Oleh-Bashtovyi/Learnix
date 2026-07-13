export const AI_CHAT_LIMITS = {
    /** AI tutor prompt. Mirrors AiChatConstants.MessageMaxLength. */
    MESSAGE_MAX: 4000,
} as const;

/**
 * Tool names the backend advertises to the model. They arrive here inside SSE `tool_use_start` events,
 * so they are a wire contract — keep them in sync with `ChatToolNames` on the backend.
 */
export const AI_CHAT_TOOLS = {
    searchCourses: 'search_courses',
    getCategories: 'get_categories',
    getInstructorCourses: 'get_instructor_courses',
    getMyLearningProfile: 'get_my_learning_profile',
    getPlatformInfo: 'get_platform_info',
    getCurrentLesson: 'get_current_lesson',
    getMyTestReview: 'get_my_test_review',
} as const;
