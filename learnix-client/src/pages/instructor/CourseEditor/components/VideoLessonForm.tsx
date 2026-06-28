import { useEffect } from 'react';
import { useForm, useWatch } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { zodResolver } from '@hookform/resolvers/zod';
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
    const {
        register,
        handleSubmit,
        setValue,
        control,
        formState: { errors, isDirty },
    } = useForm<VideoLessonFormData>({
        resolver: zodResolver(videoLessonSchema),
        defaultValues: {
            title: lesson?.title ?? '',
            videoUrl: lesson?.videoUrl ?? '',
            description: lesson?.description ?? '',
            durationSeconds: lesson?.durationSeconds ?? undefined,
        },
    });

    useEffect(() => {
        onDirtyChange?.(isDirty);
    }, [isDirty, onDirtyChange]);

    const videoUrl = useWatch({ control, name: 'videoUrl' });
    const title = useWatch({ control, name: 'title' }) || '';
    const description = useWatch({ control, name: 'description' }) || '';

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

            <div>
                <div className="mb-1 flex items-center justify-between">
                    <label className="block text-sm font-medium">{t('fieldDescription')}</label>
                    <span className="text-xs text-muted-foreground">
                        {description.length} / {LESSON_LIMITS.DESCRIPTION_MAX}
                    </span>
                </div>
                <textarea
                    {...register('description')}
                    rows={3}
                    maxLength={LESSON_LIMITS.DESCRIPTION_MAX}
                    className="w-full resize-none rounded-lg border border-input bg-background px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-ring"
                />
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
