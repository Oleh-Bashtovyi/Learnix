import { useEffect } from 'react';
import { useForm, Controller } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import MDEditor from '@uiw/react-md-editor';
import { useTranslation } from 'react-i18next';
import { postLessonSchema, type PostLessonFormData } from '@/schemas/lesson.schema';
import { LESSON_LIMITS } from '@/const/lesson.constants';
import type { CourseForEditLessonDto } from '@/types/course.types';

interface Props {
    lesson?: CourseForEditLessonDto;
    isPending: boolean;
    onSubmit: (data: PostLessonFormData) => void;
    onCancel: () => void;
    onDirtyChange?: (isDirty: boolean) => void;
}

export function PostLessonForm({ lesson, isPending, onSubmit, onCancel, onDirtyChange }: Props) {
    const { t } = useTranslation('instructor');
    const {
        register,
        handleSubmit,
        control,
        watch,
        formState: { errors, isDirty },
    } = useForm<PostLessonFormData>({
        resolver: zodResolver(postLessonSchema),
        defaultValues: {
            title: lesson?.title ?? '',
            content: lesson?.content ?? '',
        },
    });

    useEffect(() => {
        onDirtyChange?.(isDirty);
    }, [isDirty, onDirtyChange]);

    const title = watch('title') || '';
    const content = watch('content') || '';

    return (
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
            <div>
                <div className="mb-1 flex items-center justify-between">
                    <label className="block text-sm font-medium">{t('fieldTitle')}</label>
                    <span className="text-xs text-muted-foreground">
                        {title.length} / {LESSON_LIMITS.TITLE_MAX}
                    </span>
                </div>
                <input
                    {...register('title')}
                    placeholder={t('fieldTitlePlaceholder')}
                    maxLength={LESSON_LIMITS.TITLE_MAX}
                    className="w-full rounded-lg border border-input bg-background px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-ring"
                />
                {errors.title && (
                    <p className="mt-1 text-xs text-destructive">{errors.title.message}</p>
                )}
            </div>

            <div>
                <div className="mb-1 flex items-center justify-between">
                    <label className="block text-sm font-medium">{t('fieldContent')}</label>
                    <span className={`text-xs ${content.length > LESSON_LIMITS.POST_CONTENT_MAX ? 'text-destructive' : 'text-muted-foreground'}`}>
                        {content.length} / {LESSON_LIMITS.POST_CONTENT_MAX}
                    </span>
                </div>
                <Controller
                    control={control}
                    name="content"
                    render={({ field }) => (
                        <div data-color-mode="light">
                            <MDEditor
                                value={field.value}
                                onChange={(val) => {
                                    const newVal = val ?? '';
                                    if (newVal.length <= LESSON_LIMITS.POST_CONTENT_MAX) {
                                        field.onChange(newVal);
                                    } else {
                                        field.onChange(newVal.slice(0, LESSON_LIMITS.POST_CONTENT_MAX));
                                    }
                                }}
                                height={300}
                                preview="edit"
                            />
                        </div>
                    )}
                />
                {errors.content && (
                    <p className="mt-1 text-xs text-destructive">{errors.content.message}</p>
                )}
            </div>

            <div className="flex justify-end gap-2 pt-2">
                <button
                    type="button"
                    onClick={onCancel}
                    className="rounded-lg border border-border px-4 py-2 text-sm hover:bg-secondary"
                >
                    {t('btnCancel')}
                </button>
                <button
                    type="submit"
                    disabled={isPending || !isDirty}
                    className="rounded-lg bg-primary px-4 py-2 text-sm font-medium text-primary-foreground hover:bg-primary/90 disabled:cursor-not-allowed disabled:opacity-60"
                >
                    {isPending ? '...' : t('btnSaveLesson')}
                </button>
            </div>
        </form>
    );
}
