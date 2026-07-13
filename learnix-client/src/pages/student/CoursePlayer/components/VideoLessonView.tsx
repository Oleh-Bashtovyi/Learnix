import { useEffect, useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { PlayCircle, X } from 'lucide-react';
import { useLessonContent } from '@/hooks/lesson/useLessonContent';
import { usePlayerStore } from '@/store/player.store';
import type { LessonProgressItemDto } from '@/types/progress.types';
import { cn } from '@/utils/cn';

interface VideoLessonViewProps {
    lesson: LessonProgressItemDto;
    courseId: string;
    nextLessonTitle?: string;
    /** Called when the video crosses the auto-complete threshold (mark as done, no redirect). */
    onVideoNearEnd?: () => void;
    /** Called when the video plays all the way to the end and countdown finishes, or user clicks Play Next. */
    onPlayNext?: () => void;
}

export function VideoLessonView({
    lesson,
    courseId,
    nextLessonTitle,
    onVideoNearEnd,
    onPlayNext,
}: VideoLessonViewProps) {
    const { t } = useTranslation('lessonPlayer');
    const { data, isLoading } = useLessonContent(courseId, lesson.lessonId);
    const hasAutoCompleted = useRef(false);

    const { autoplay, toggleAutoplay } = usePlayerStore();
    const [showOverlay, setShowOverlay] = useState(false);
    const [countdown, setCountdown] = useState(5);
    const [isCancelled, setIsCancelled] = useState(false);

    useEffect(() => {
        let timer: ReturnType<typeof setTimeout>;
        if (showOverlay && autoplay && !isCancelled && countdown > 0) {
            timer = setTimeout(() => {
                setCountdown((prev) => prev - 1);
            }, 1000);
        } else if (showOverlay && autoplay && !isCancelled && countdown === 0) {
            onPlayNext?.();
        }
        return () => clearTimeout(timer);
    }, [showOverlay, countdown, autoplay, isCancelled, onPlayNext]);

    const handleTimeUpdate = (e: React.SyntheticEvent<HTMLVideoElement>) => {
        if (!onVideoNearEnd || hasAutoCompleted.current) return;

        const video = e.currentTarget;
        if (!video.duration) return;

        const timeRemaining = video.duration - video.currentTime;
        const threshold = Math.min(120, video.duration * 0.2);

        if (timeRemaining <= threshold) {
            hasAutoCompleted.current = true;
            onVideoNearEnd();
        }
    };

    const handleEnded = () => {
        if (!hasAutoCompleted.current && onVideoNearEnd) {
            hasAutoCompleted.current = true;
            onVideoNearEnd();
        }

        if (nextLessonTitle && onPlayNext) {
            setShowOverlay(true);
            setCountdown(5);
            setIsCancelled(false);
        }
    };

    return (
        <div className="mx-auto max-w-5xl">
            {isLoading && (
                <div className="flex aspect-video w-full animate-pulse items-center justify-center rounded-xl bg-secondary" />
            )}

            {!isLoading && data?.videoUrl && (
                <div className="relative overflow-hidden rounded-xl bg-black shadow-lg">
                    <video
                        key={data.videoUrl}
                        controls
                        className="aspect-video w-full"
                        preload="metadata"
                        onEnded={handleEnded}
                        onTimeUpdate={handleTimeUpdate}
                    >
                        <source src={data.videoUrl} />
                        Your browser does not support the video element.
                    </video>

                    {showOverlay && nextLessonTitle && (
                        <div className="animate-in fade-in zoom-in-95 absolute inset-0 z-10 flex flex-col items-center justify-center bg-black/80 px-4 text-center text-white backdrop-blur-sm transition-all duration-300 sm:px-6">
                            <h3 className="mb-1 line-clamp-2 max-w-md text-sm font-medium text-white/80 sm:mb-2 sm:text-xl">
                                {t('autoplay.upNext', { title: nextLessonTitle })}
                            </h3>

                            {autoplay && !isCancelled ? (
                                <p className="mb-4 text-lg font-bold tracking-tight sm:mb-8 sm:text-3xl">
                                    {t('autoplay.startingIn', { seconds: countdown })}
                                </p>
                            ) : (
                                <div className="mb-4 h-6 sm:mb-8 sm:h-10" />
                            )}

                            <div className="flex items-center gap-2 sm:gap-4">
                                {autoplay && !isCancelled && (
                                    <button
                                        onClick={() => setIsCancelled(true)}
                                        className="rounded-full bg-white/10 px-4 py-1.5 text-xs font-medium text-white transition-colors hover:bg-white/20 sm:px-6 sm:py-2.5 sm:text-base"
                                    >
                                        {t('common:actions.cancel')}
                                    </button>
                                )}
                                <button
                                    onClick={() => onPlayNext?.()}
                                    className="flex items-center gap-1.5 rounded-full bg-primary px-4 py-1.5 text-xs font-medium text-primary-foreground shadow-lg transition-transform hover:scale-105 hover:bg-primary/90 sm:gap-2 sm:px-8 sm:py-2.5 sm:text-base"
                                >
                                    <PlayCircle className="size-4 sm:size-5" />
                                    {t('autoplay.playNow')}
                                </button>
                            </div>

                            <button
                                onClick={() => setShowOverlay(false)}
                                className="absolute right-2 top-2 rounded-full bg-white/10 p-1.5 text-white/70 transition-colors hover:bg-white/20 hover:text-white sm:right-4 sm:top-4 sm:p-2"
                            >
                                <X className="size-4 sm:size-5" />
                            </button>
                        </div>
                    )}
                </div>
            )}

            {!isLoading && !data?.videoUrl && (
                <div className="flex aspect-video w-full items-center justify-center rounded-xl border border-dashed border-border bg-secondary/40 text-sm text-muted-foreground">
                    {t('video.noVideoAttached')}
                </div>
            )}

            {/* Autoplay Toggle */}
            <div className="mt-4 flex justify-end">
                <div className="flex items-center gap-2.5 rounded-full bg-secondary/50 px-3 py-1.5 transition-colors hover:bg-secondary">
                    <span className="text-sm font-medium text-muted-foreground">
                        {t('autoplay.label', 'Autoplay')}
                    </span>
                    <button
                        role="switch"
                        aria-checked={autoplay}
                        onClick={toggleAutoplay}
                        className={cn(
                            'relative inline-flex h-5 w-9 shrink-0 cursor-pointer items-center rounded-full transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary focus-visible:ring-offset-2 focus-visible:ring-offset-background',
                            autoplay ? 'bg-primary' : 'bg-muted-foreground/30',
                        )}
                    >
                        <span
                            className={cn(
                                'pointer-events-none block h-4 w-4 rounded-full bg-background shadow-sm ring-0 transition-transform',
                                autoplay ? 'translate-x-4' : 'translate-x-0.5',
                            )}
                        />
                    </button>
                </div>
            </div>

            <div className="mt-6">
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
