export type LessonType = 'Video' | 'Post' | 'Test';

export interface LessonContentDto {
    lessonId: string;
    title: string;
    lessonType: LessonType;
    // Video only
    videoUrl: string | null;
    description: string | null;
    durationSeconds: number | null;
    // Post only
    content: string | null;
}
export type QuestionType = 'SingleChoice' | 'MultipleChoice' | 'TextInput';

export interface QuestionOptionDto {
    text: string;
    order: number;
}

export interface QuestionDto {
    text: string;
    type: QuestionType;
    order: number;
    options: QuestionOptionDto[] | null;
}

export interface LatestAttemptDto {
    attemptId: string;
    attemptNumber: number;
    score: number;
    maxScore: number;
    passed: boolean;
    submittedAt: string;
}

export interface StudentTestStatusDto {
    attemptsUsed: number;
    canAttempt: boolean;
    cooldownRemainingMinutes: number | null;
    /** ID of an in-progress (not yet submitted) attempt, if one exists. */
    inProgressAttemptId: string | null;
    latestAttempt: LatestAttemptDto | null;
}

export interface GetTestLessonDto {
    lessonId: string;
    title: string;
    description: string | null;
    passingThreshold: number;
    attemptLimit: number | null;
    cooldownMinutes: number | null;
    studentStatus: StudentTestStatusDto;
    questions: QuestionDto[];
}

export interface SubmittedAnswerDto {
    questionOrder: number;
    selectedOptionOrders: number[];
    textValue: string | null;
}

export interface SubmitAttemptRequest {
    answers: SubmittedAnswerDto[];
}

export interface QuestionResultDto {
    questionOrder: number;
    isCorrect: boolean;
    /** Option orders that are correct. Null for TextInput questions. */
    correctOptionOrders: number[] | null;
    /** The correct text answer. Null for choice questions. */
    correctTextAnswer: string | null;
}

export interface SubmitAttemptResponse {
    attemptId: string;
    attemptNumber: number;
    score: number;
    maxScore: number;
    passed: boolean;
    submittedAt: string;
    questionResults: QuestionResultDto[];
}

export interface TestAttemptSummaryDto {
    attemptId: string;
    attemptNumber: number;
    score: number;
    maxScore: number;
    passed: boolean;
    startedAt: string;
    submittedAt: string;
}

export interface StartAttemptResponse {
    attemptId: string;
    attemptNumber: number;
    startedAt: string;
}
