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
