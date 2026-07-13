import { LessonType, QuestionType, TestReviewMode } from '@/enums/lesson.enums';

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
    /** How much of a submitted attempt this test discloses. */
    reviewMode: TestReviewMode;
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
    /** Null when the review mode withholds correctness (AnswersOnly). */
    isCorrect: boolean | null;
    /** Option orders that are correct. Null for TextInput questions, and below FullReview. */
    correctOptionOrders: number[] | null;
    /** The correct text answer. Null for choice questions, and below FullReview. */
    correctTextAnswer: string | null;
}

export interface SubmitAttemptResponse {
    attemptId: string;
    attemptNumber: number;
    score: number;
    maxScore: number;
    passed: boolean;
    submittedAt: string;
    /** Why questionResults may be empty or partial — the instructor's choice, not a missing field. */
    reviewMode: TestReviewMode;
    questionResults: QuestionResultDto[];
}

/** A past attempt replayed: the questions, what the student answered, and as much marking as allowed. */
export interface TestAttemptReviewDto {
    attemptId: string;
    attemptNumber: number;
    score: number;
    maxScore: number;
    passed: boolean;
    startedAt: string;
    submittedAt: string;
    reviewMode: TestReviewMode;
    /** Empty when the mode is ScoreOnly. */
    questions: ReviewedQuestionDto[];
}

export interface ReviewedQuestionDto {
    order: number;
    text: string;
    type: QuestionType;
    /** False when the student skipped the question entirely. */
    answered: boolean;
    /** Null when the mode withholds correctness. */
    isCorrect: boolean | null;
    /** Null for TextInput questions. */
    options: ReviewedOptionDto[] | null;
    studentSelectedOptionOrders: number[] | null;
    studentTextAnswer: string | null;
    /** Null unless the mode discloses correct answers. */
    correctTextAnswer: string | null;
}

export interface ReviewedOptionDto {
    order: number;
    text: string;
    /** Null unless the mode discloses correct answers. */
    isCorrect: boolean | null;
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
