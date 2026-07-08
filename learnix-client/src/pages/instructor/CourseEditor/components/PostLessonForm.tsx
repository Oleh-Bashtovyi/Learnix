import { useEffect } from 'react';
import { Controller, useForm, useWatch } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { zodResolver } from '@hookform/resolvers/zod';
import MDEditor from '@uiw/react-md-editor';
import { FormInput } from '@/components/common/form/FormInput';
import { LESSON_LIMITS } from '@/const/lesson.constants';
import { type PostLessonFormData, postLessonSchema } from '@/schemas/lesson.schema';
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

    const title = useWatch({ control, name: 'title' }) || '';
    const content = useWatch({ control, name: 'content' }) || '';

    return (
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
            <FormInput
                label={
                    <div className="flex w-full items-center justify-between">
                        <span>{t('fieldTitle')}</span>
                        <span className="text-xs font-normal text-muted-foreground">
                            {title.length} / {LESSON_LIMITS.TITLE_MAX}
                        </span>
                    </div>
                }
                containerClassName="[&>label]:w-full"
                placeholder={t('fieldTitlePlaceholder')}
                maxLength={LESSON_LIMITS.TITLE_MAX}
                error={errors.title?.message}
                {...register('title')}
            />

            <div>
                <div className="mb-1 flex items-center justify-between">
                    <label className="block text-sm font-medium">{t('fieldContent')}</label>
                    <span
                        className={`text-xs ${content.length > LESSON_LIMITS.POST_CONTENT_MAX ? 'text-destructive' : 'text-muted-foreground'}`}
                    >
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
                                        field.onChange(
                                            newVal.slice(0, LESSON_LIMITS.POST_CONTENT_MAX),
                                        );
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
                    {t('common:actions.cancel')}
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
