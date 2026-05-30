import { z } from 'zod';

export const videoLessonSchema = z.object({
    title: z.string().min(1, 'Title is required').max(300, 'Title is too long'),
    videoUrl: z.string().url('Video URL is invalid').min(1, 'Video is required'),
    description: z.string().max(2000, 'Description is too long').optional(),
    durationSeconds: z.coerce.number().int().min(1).optional(),
});

export const postLessonSchema = z.object({
    title: z.string().min(1, 'Title is required').max(300, 'Title is too long'),
    content: z.string().min(1, 'Content is required').max(50000, 'Content is too long'),
});

const questionOptionSchema = z.object({
    text: z.string().min(1, 'Option text is required'),
    isCorrect: z.boolean(),
});

const questionSchema = z.object({
    text: z.string().min(1, 'Question text is required'),
    type: z.enum(['SingleChoice', 'MultipleChoice']),
    options: z
        .array(questionOptionSchema)
        .min(2, 'At least 2 options required')
        .max(6, 'Maximum 6 options'),
});

export const testLessonSchema = z.object({
    title: z.string().min(1, 'Title is required').max(300, 'Title is too long'),
    description: z.string().max(2000, 'Description is too long').optional(),
    passingThreshold: z.coerce
        .number()
        .int()
        .min(1, 'Must be at least 1%')
        .max(100, 'Cannot exceed 100%'),
    attemptLimit: z.coerce.number().int().min(1).optional(),
    cooldownMinutes: z.coerce.number().int().min(1).optional(),
    questions: z.array(questionSchema).min(1, 'At least one question required'),
});

export type VideoLessonFormData = z.infer<typeof videoLessonSchema>;
export type PostLessonFormData = z.infer<typeof postLessonSchema>;
export type TestLessonFormData = z.infer<typeof testLessonSchema>;
