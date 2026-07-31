import { useEffect } from 'react';
import { Controller, FormProvider, useForm } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { zodResolver } from '@hookform/resolvers/zod';
import MDEditor from '@uiw/react-md-editor';
import { CharCounter } from '@/components/common/form/CharCounter';
import { FormInput } from '@/components/common/form/FormInput';
import { AsyncButton } from '@/components/ui/async-button';
import { Button } from '@/components/ui/button';
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
    const form = useForm<PostLessonFormData>({
        resolver: zodResolver(postLessonSchema),
        defaultValues: {
            title: lesson?.title ?? '',
            content: lesson?.content ?? '',
        },
    });

    const {
        register,
        handleSubmit,
        control,
        formState: { errors, isDirty },
    } = form;

    useEffect(() => {
        onDirtyChange?.(isDirty);
    }, [isDirty, onDirtyChange]);

    return (
        // FormProvider so the char counters can read the live field values.
        <FormProvider {...form}>
            <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
                <FormInput
                    variant="card"
                    label={t('fieldTitle')}
                    placeholder={t('fieldTitlePlaceholder')}
                    maxLength={LESSON_LIMITS.TITLE_MAX}
                    showCharLimit
                    error={errors.title?.message}
                    {...register('title')}
                />

                <div>
                    {/* MDEditor is third-party, so it gets the counter next to its label rather than
                    through the FormInput/FormTextarea `showCharLimit` slot. */}
                    <div className="mb-1 flex items-center justify-between gap-2">
                        <label className="block text-sm font-medium">{t('fieldContent')}</label>
                        <CharCounter name="content" max={LESSON_LIMITS.POST_CONTENT_MAX} />
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
                    <Button type="button" variant="outline" onClick={onCancel}>
                        {t('common:actions.cancel')}
                    </Button>
                    <AsyncButton
                        type="submit"
                        disabled={!isDirty}
                        isLoading={isPending}
                        loadingText={t('common:actions.saving')}
                    >
                        {t('btnSaveLesson')}
                    </AsyncButton>
                </div>
            </form>
        </FormProvider>
    );
}
