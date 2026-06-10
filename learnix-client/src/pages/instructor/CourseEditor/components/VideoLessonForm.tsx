import { useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useTranslation } from 'react-i18next';
import { videoLessonSchema, type VideoLessonFormData } from '@/schemas/lesson.schema';
import { VideoUploader } from './VideoUploader';
import type { CourseForEditLessonDto } from '@/types/course.types';

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
        watch,
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

    const videoUrl = watch('videoUrl');

    return (
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
            <div>
                <label className="mb-1 block text-sm font-medium">{t('fieldTitle')}</label>
                <input
                    {...register('title')}
                    placeholder={t('fieldTitlePlaceholder')}
                    className="w-full rounded-lg border border-input bg-background px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-ring"
                />
                {errors.title && (
                    <p className="mt-1 text-xs text-destructive">{errors.title.message}</p>
                )}
            </div>

            <VideoUploader
                value={videoUrl}
                onChange={(path) => setValue('videoUrl', path, { shouldValidate: true })}
            />
            {errors.videoUrl && (
                <p className="text-xs text-destructive">{errors.videoUrl.message}</p>
            )}

            <div>
                <label className="mb-1 block text-sm font-medium">{t('fieldDescription')}</label>
                <textarea
                    {...register('description')}
                    rows={3}
                    className="w-full resize-none rounded-lg border border-input bg-background px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-ring"
                />
            </div>

            <div>
                <label className="mb-1 block text-sm font-medium">{t('fieldDuration')}</label>
                <input
                    {...register('durationSeconds')}
                    type="number"
                    min={1}
                    className="w-full rounded-lg border border-input bg-background px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-ring"
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
