import { z } from 'zod';
import { LESSON_LIMITS } from '@/const/lesson.constants';
import { QUESTION_TYPES } from '@/types/lesson.types';

export const videoLessonSchema = z.object({
    title: z
        .string()
        .trim()
        .min(1, 'Title is required')
        .max(LESSON_LIMITS.TITLE_MAX, 'Title is too long'),
    videoUrl: z.string().trim().min(1, 'Video is required'),
    description: z
        .string()
        .max(LESSON_LIMITS.DESCRIPTION_MAX, 'Description is too long')
        .optional(),
    durationSeconds: z.number().int().min(1).optional(),
});

export const postLessonSchema = z.object({
    title: z
        .string()
        .trim()
        .min(1, 'Title is required')
        .max(LESSON_LIMITS.TITLE_MAX, 'Title is too long'),
    content: z
        .string()
        .trim()
        .min(1, 'Content is required')
        .max(LESSON_LIMITS.POST_CONTENT_MAX, 'Content is too long'),
});

const questionOptionSchema = z.object({
    text: z.string().trim().min(1, 'Option text is required'),
    isCorrect: z.boolean(),
});

const textAnswerSchema = z.object({
    correctAnswer: z.string().trim().min(1, 'Correct answer is required'),
    ignoreCase: z.boolean(),
    allowFuzzy: z.boolean(),
});

const questionSchema = z
    .object({
        text: z.string().trim().min(1, 'Question text is required'),
        type: z.enum(QUESTION_TYPES),
        options: z.array(questionOptionSchema),
        textAnswer: textAnswerSchema.optional(),
    })
    .superRefine((data, ctx) => {
        if (data.type === 'TextInput') {
            if (
                !data.textAnswer ||
                !data.textAnswer.correctAnswer ||
                data.textAnswer.correctAnswer.trim() === ''
            ) {
                ctx.addIssue({
                    code: z.ZodIssueCode.custom,
                    message: 'Correct answer is required',
                    path: ['textAnswer', 'correctAnswer'],
                });
            }
        } else {
            if (!data.options || data.options.length < LESSON_LIMITS.QUESTION_OPTIONS_MIN) {
                ctx.addIssue({
                    code: z.ZodIssueCode.custom,
                    message: `At least ${LESSON_LIMITS.QUESTION_OPTIONS_MIN} options required`,
                    path: ['options'],
                });
            } else if (data.options.length > LESSON_LIMITS.QUESTION_OPTIONS_MAX) {
                ctx.addIssue({
                    code: z.ZodIssueCode.custom,
                    message: `Maximum ${LESSON_LIMITS.QUESTION_OPTIONS_MAX} options`,
                    path: ['options'],
                });
            }
        }
    });

export const testLessonSchema = z.object({
    title: z
        .string()
        .trim()
        .min(1, 'Title is required')
        .max(LESSON_LIMITS.TITLE_MAX, 'Title is too long'),
    description: z
        .string()
        .max(LESSON_LIMITS.DESCRIPTION_MAX, 'Description is too long')
        .optional(),
    passingThreshold: z
        .number()
        .int()
        .min(
            LESSON_LIMITS.PASSING_THRESHOLD_MIN,
            `Must be at least ${LESSON_LIMITS.PASSING_THRESHOLD_MIN}%`,
        )
        .max(
            LESSON_LIMITS.PASSING_THRESHOLD_MAX,
            `Cannot exceed ${LESSON_LIMITS.PASSING_THRESHOLD_MAX}%`,
        ),
    attemptLimit: z.number().int().min(LESSON_LIMITS.ATTEMPT_LIMIT_MIN).optional(),
    cooldownMinutes: z.number().int().min(LESSON_LIMITS.COOLDOWN_MINUTES_MIN).optional(),
    questions: z.array(questionSchema).min(1, 'At least one question required'),
});

export type VideoLessonFormData = z.infer<typeof videoLessonSchema>;
export type PostLessonFormData = z.infer<typeof postLessonSchema>;
export type TestLessonFormData = z.infer<typeof testLessonSchema>;
