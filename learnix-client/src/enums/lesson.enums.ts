export const LessonType = {
    Video: 'Video',
    Post: 'Post',
    Test: 'Test',
} as const;
export type LessonType = (typeof LessonType)[keyof typeof LessonType];

export const QuestionType = {
    SingleChoice: 'SingleChoice',
    MultipleChoice: 'MultipleChoice',
    TextInput: 'TextInput',
} as const;
export type QuestionType = (typeof QuestionType)[keyof typeof QuestionType];

/**
 * How much of a submitted attempt the instructor lets the student see back. Mirrors
 * Learnix.Domain.Enums.TestReviewMode — a ladder, where each value discloses everything the one
 * before it does, plus one thing more.
 */
export const TestReviewMode = {
    /** Score and pass/fail only. The questions do not come back at all. */
    ScoreOnly: 'ScoreOnly',
    /** The questions, and what the student answered. Not whether any of it was right. */
    AnswersOnly: 'AnswersOnly',
    /** Also which questions were wrong — but not what the right answer was. */
    AnswersAndCorrectness: 'AnswersAndCorrectness',
    /** The whole attempt, correct answers included. The default. */
    FullReview: 'FullReview',
} as const;
export type TestReviewMode = (typeof TestReviewMode)[keyof typeof TestReviewMode];
