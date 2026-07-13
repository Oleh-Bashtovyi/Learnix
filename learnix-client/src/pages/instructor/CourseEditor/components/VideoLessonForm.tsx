import { useEffect } from 'react';
import { FormProvider, useForm, useWatch } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { zodResolver } from '@hookform/resolvers/zod';
import { FormInput } from '@/components/common/form/FormInput';
import { FormTextarea } from '@/components/common/form/FormTextarea';
import { LESSON_LIMITS } from '@/const/lesson.constants';
import { type VideoLessonFormData, videoLessonSchema } from '@/schemas/lesson.schema';
import type { CourseForEditLessonDto } from '@/types/course.types';
import { VideoUploader } from './VideoUploader';

interface Props {
    lesson?: CourseForEditLessonDto;
    isPending: boolean;
    onSubmit: (data: VideoLessonFormData) => void;
    onCancel: () => void;
    onDirtyChange?: (isDirty: boolean) => void;
}

export function VideoLessonForm({ lesson, isPending, onSubmit, onCancel, onDirtyChange }: Props) {
    const { t } = useTranslation('instructor');
    const form = useForm<VideoLessonFormData>({
        resolver: zodResolver(videoLessonSchema),
        defaultValues: {
            title: lesson?.title ?? '',
            videoUrl: lesson?.videoUrl ?? '',
            description: lesson?.description ?? '',
            durationSeconds: lesson?.durationSeconds ?? undefined,
        },
    });

    const {
        register,
        handleSubmit,
        setValue,
        control,
        formState: { errors, isDirty },
    } = form;

    useEffect(() => {
        onDirtyChange?.(isDirty);
    }, [isDirty, onDirtyChange]);

    const videoUrl = useWatch({ control, name: 'videoUrl' });

    return (
        // FormProvider so the char counters inside the fields can read the live field values.
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

                <VideoUploader
                    value={videoUrl}
                    onChange={(path, duration) => {
                        setValue('videoUrl', path, { shouldValidate: true });
                        if (duration) setValue('durationSeconds', duration, { shouldDirty: true });
                    }}
                />
                {errors.videoUrl && (
                    <p className="text-xs text-destructive">{errors.videoUrl.message}</p>
                )}

                <FormTextarea
                    variant="card"
                    label={t('fieldDescription')}
                    rows={3}
                    maxLength={LESSON_LIMITS.DESCRIPTION_MAX}
                    showCharLimit
                    error={errors.description?.message}
                    {...register('description')}
                />

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
        </FormProvider>
    );
}
