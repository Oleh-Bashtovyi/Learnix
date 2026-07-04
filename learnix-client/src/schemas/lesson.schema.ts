import { z } from 'zod';
import { LESSON_LIMITS } from '@/const/lesson.constants';
import { QuestionType } from '@/enums/lesson.enums';

/**
 * Related ADRs:
 * - ADR-FRONT-FORMS-002: Zod Schemas as Source of Truth
 */
export const videoLessonSchema = z.object({
    title: z.string().trim().min(1).max(LESSON_LIMITS.TITLE_MAX),
    videoUrl: z.string().trim().min(1),
    description: z.string().max(LESSON_LIMITS.DESCRIPTION_MAX).optional(),
    durationSeconds: z.number().int().min(1).optional(),
});

export const postLessonSchema = z.object({
    title: z.string().trim().min(1).max(LESSON_LIMITS.TITLE_MAX),
    content: z.string().trim().min(1).max(LESSON_LIMITS.POST_CONTENT_MAX),
});

const questionOptionSchema = z.object({
    text: z.string().trim().min(1),
    isCorrect: z.boolean(),
});

const textAnswerSchema = z.object({
    correctAnswer: z.string().trim().min(1),
    ignoreCase: z.boolean(),
    allowFuzzy: z.boolean(),
});

const baseQuestionSchema = z.object({
    text: z.string().trim().min(1),
});

const singleChoiceQuestionSchema = baseQuestionSchema.extend({
    type: z.literal(QuestionType.SingleChoice),
    options: z
        .array(questionOptionSchema)
        .min(LESSON_LIMITS.QUESTION_OPTIONS_MIN)
        .max(LESSON_LIMITS.QUESTION_OPTIONS_MAX),
});

const multipleChoiceQuestionSchema = baseQuestionSchema.extend({
    type: z.literal(QuestionType.MultipleChoice),
    options: z
        .array(questionOptionSchema)
        .min(LESSON_LIMITS.QUESTION_OPTIONS_MIN)
        .max(LESSON_LIMITS.QUESTION_OPTIONS_MAX),
});

const textInputQuestionSchema = baseQuestionSchema.extend({
    type: z.literal(QuestionType.TextInput),
    textAnswer: textAnswerSchema,
});

const questionSchema = z.discriminatedUnion('type', [
    singleChoiceQuestionSchema,
    multipleChoiceQuestionSchema,
    textInputQuestionSchema,
]);

export const testLessonSchema = z.object({
    title: z.string().trim().min(1).max(LESSON_LIMITS.TITLE_MAX),
    description: z.string().max(LESSON_LIMITS.DESCRIPTION_MAX).optional(),
    passingThreshold: z
        .number()
        .int()
        .min(LESSON_LIMITS.PASSING_THRESHOLD_MIN)
        .max(LESSON_LIMITS.PASSING_THRESHOLD_MAX),
    attemptLimit: z.number().int().min(LESSON_LIMITS.ATTEMPT_LIMIT_MIN).optional(),
    cooldownMinutes: z.number().int().min(LESSON_LIMITS.COOLDOWN_MINUTES_MIN).optional(),
    questions: z.array(questionSchema).min(1),
});

export type VideoLessonFormData = z.infer<typeof videoLessonSchema>;
export type PostLessonFormData = z.infer<typeof postLessonSchema>;
export type TestLessonFormData = z.infer<typeof testLessonSchema>;
