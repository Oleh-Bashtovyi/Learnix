import { useRef, useEffect } from 'react';
import { useLessonContent } from '@/hooks/useLessonContent';
import type { LessonProgressItemDto } from '@/types/progress.types';

interface VideoLessonViewProps {
    lesson: LessonProgressItemDto;
    courseId: string;
    onVideoEnded?: () => void;
}

export function VideoLessonView({ lesson, courseId, onVideoEnded }: VideoLessonViewProps) {
    const { data, isLoading } = useLessonContent(courseId, lesson.lessonId);
    const hasAutoCompleted = useRef(false);

    useEffect(() => {
        hasAutoCompleted.current = false;
    }, [lesson.lessonId]);

    const handleTimeUpdate = (e: React.SyntheticEvent<HTMLVideoElement>) => {
        if (!onVideoEnded || hasAutoCompleted.current) return;

        const video = e.currentTarget;
        if (!video.duration) return;

        const timeRemaining = video.duration - video.currentTime;
        const percentageCompleted = video.currentTime / video.duration;

        // Auto-complete if less than 10 seconds remaining, or 95% completed
        if (timeRemaining < 10 || percentageCompleted > 0.95) {
            hasAutoCompleted.current = true;
            onVideoEnded();
        }
    };

    return (
        <div className="mx-auto max-w-5xl">
            {isLoading && (
                <div className="flex aspect-video w-full animate-pulse items-center justify-center rounded-xl bg-secondary" />
            )}

            {!isLoading && data?.videoUrl && (
                <video
                    key={data.videoUrl}
                    controls
                    className="aspect-video w-full rounded-xl bg-black shadow-lg"
                    preload="metadata"
                    onEnded={() => {
                        if (!hasAutoCompleted.current && onVideoEnded) {
                            hasAutoCompleted.current = true;
                            onVideoEnded();
                        }
                    }}
                    onTimeUpdate={handleTimeUpdate}
                >
                    <source src={data.videoUrl} />
                    Your browser does not support the video element.
                </video>
            )}

            {!isLoading && !data?.videoUrl && (
                <div className="flex aspect-video w-full items-center justify-center rounded-xl border border-dashed border-border bg-secondary/40 text-sm text-muted-foreground">
                    No video attached to this lesson yet.
                </div>
            )}

            <div className="mt-8">
                <h1 className="font-heading text-2xl font-bold text-foreground">{lesson.title}</h1>
                {!isLoading && data?.description && (
                    <p className="mt-3 text-base leading-relaxed text-muted-foreground">
                        {data.description}
                    </p>
                )}
            </div>
        </div>
    );
}
