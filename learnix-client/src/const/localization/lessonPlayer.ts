export const LESSON_PLAYER = {
    HEADER: {
        backToCatalog: 'Back to courses',
        chatDisabledTooltip: 'Chat with instructor (coming soon)',
    },
    SIDEBAR: {
        progressLabel: (completed: number, total: number) => `${completed} / ${total} completed`,
        sectionPrefix: 'Section',
    },
    LESSON_TYPE: {
        Video: 'Video',
        Post: 'Article',
        Test: 'Test',
    },
    ACTIONS: {
        markComplete: 'Mark as complete',
        completed: 'Completed',
        previousLesson: 'Previous',
        nextLesson: 'Next',
        takeTest: 'Take Test',
        viewResults: 'View Results',
    },
    VIDEO: {
        noVideoAttached: 'No video attached to this lesson yet.',
    },
    POST: {
        noContent: 'No content for this lesson yet.',
    },
    TEST_PREVIEW: {
        heading: 'Test lesson',
        questionsCount: (n: number) => `${n} question${n === 1 ? '' : 's'}`,
        passThreshold: (pct: number) => `Passing score: ${pct}%`,
        attemptsLimit: (n: number) => `Attempt limit: ${n}`,
        unlimitedAttempts: 'Unlimited attempts',
        attemptsUsed: (n: number) => `Attempts used: ${n}`,
        cooldownRemaining: (min: number) => `Next attempt in ${min} min`,
        lastResult: 'Last result',
        score: (score: number, max: number) => `${score} / ${max}`,
        passed: 'Passed',
        failed: 'Failed',
        startTest: 'Start test',
        retakeTest: 'Retake test',
    },
    LOADING: 'Loading lesson...',
    LESSON_NOT_FOUND: 'Lesson not found in this course.',
} as const;
