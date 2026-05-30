import { useMemo, useEffect } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { useMutation } from '@tanstack/react-query';
import { MessageSquare, ChevronLeft, ChevronRight, CheckCircle2 } from 'lucide-react';
import { cn } from '@/utils/cn';
import { messagesApi } from '@/api/messages.api';
import { useCourseDetail } from '@/hooks/useCourseDetail';
import { useCourseProgress } from '@/hooks/useCourseProgress';
import { useMarkLessonComplete } from '@/hooks/useMarkLessonComplete';
import { CourseSidebar } from './components/CourseSidebar';
import { VideoLessonView } from './components/VideoLessonView';
import { PostLessonView } from './components/PostLessonView';
import { TestLessonPreview } from './components/TestLessonPreview';
import { LESSON_PLAYER } from '@/const/localization/lessonPlayer';

export default function CoursePlayerPage() {
    const { courseId, lessonId } = useParams<{ courseId: string; lessonId: string }>();
    const navigate = useNavigate();

    const { data: course } = useCourseDetail(courseId!);
    const { data: progress, isLoading } = useCourseProgress(courseId!);
    const markComplete = useMarkLessonComplete(courseId!);

    const startChat = useMutation({
        mutationFn: () => messagesApi.startOrGet({ courseId: courseId! }),
        onSuccess: (conversation) => {
            navigate('/messages', { state: { initialConversation: conversation } });
        },
    });

    useEffect(() => {
        if (courseId && lessonId) {
            localStorage.setItem(`lastLesson_${courseId}`, lessonId);
        }
    }, [courseId, lessonId]);

    const allLessons = useMemo(
        () =>
            (progress?.sections ?? [])
                .slice()
                .sort((a, b) => a.displayOrder - b.displayOrder)
                .flatMap((s) => s.lessons.slice().sort((a, b) => a.displayOrder - b.displayOrder)),
        [progress],
    );

    const currentLesson = useMemo(
        () => allLessons.find((l) => l.lessonId === lessonId) ?? null,
        [allLessons, lessonId],
    );

    const currentIndex = allLessons.findIndex((l) => l.lessonId === lessonId);
    const prevLesson = currentIndex > 0 ? allLessons[currentIndex - 1] : null;
    const nextLesson = currentIndex < allLessons.length - 1 ? allLessons[currentIndex + 1] : null;

    const handleMarkComplete = () => {
        if (!lessonId || currentLesson?.isCompleted) return;
        markComplete.mutate(lessonId, {
            onSuccess: () => {
                if (nextLesson) {
                    navigate(`/courses/${courseId}/learn/${nextLesson.lessonId}`);
                }
            },
        });
    };

    return (
        <div className="flex h-screen flex-col overflow-hidden bg-background">
            {/* Top bar */}
            <header className="flex h-14 shrink-0 items-center justify-between border-b border-border bg-card px-4">
                <div className="flex min-w-0 items-center gap-3">
                    <Link
                        to="/"
                        className="flex shrink-0 items-center gap-2 font-heading font-bold"
                    >
                        <div className="grid h-7 w-7 place-items-center rounded-md bg-primary text-sm font-bold text-primary-foreground">
                            L
                        </div>
                        <span className="hidden text-sm sm:block">Learnix</span>
                    </Link>
                    {course && (
                        <>
                            <span className="text-border">|</span>
                            <Link
                                to={`/courses/${courseId}`}
                                className="truncate text-sm font-medium text-foreground transition-colors hover:text-primary"
                            >
                                {course.title}
                            </Link>
                        </>
                    )}
                </div>

                <div className="flex shrink-0 items-center gap-2">
                    <button
                        type="button"
                        onClick={() => startChat.mutate()}
                        disabled={startChat.isPending}
                        title={LESSON_PLAYER.HEADER.messageInstructor}
                        className="grid h-8 w-8 place-items-center rounded-lg text-muted-foreground transition-colors hover:bg-secondary hover:text-foreground disabled:opacity-50"
                    >
                        {startChat.isPending ? (
                            <div className="h-4 w-4 animate-spin rounded-full border-2 border-current border-t-transparent" />
                        ) : (
                            <MessageSquare className="h-4 w-4" />
                        )}
                    </button>
                </div>
            </header>

            {/* Content */}
            <div className="flex min-h-0 flex-1">
                {/* Sidebar */}
                <CourseSidebar
                    sections={progress?.sections ?? []}
                    currentLessonId={lessonId!}
                    courseId={courseId!}
                    totalLessons={progress?.totalLessons ?? 0}
                    completedLessons={progress?.completedLessons ?? 0}
                />

                {/* Main lesson area */}
                <div className="flex min-w-0 flex-1 flex-col overflow-hidden">
                    <main className="flex-1 overflow-y-auto p-8">
                        {isLoading && (
                            <div className="flex h-full items-center justify-center">
                                <div className="space-y-3 text-center">
                                    <div className="mx-auto h-10 w-10 animate-spin rounded-full border-2 border-primary border-t-transparent" />
                                    <p className="text-sm text-muted-foreground">
                                        {LESSON_PLAYER.LOADING}
                                    </p>
                                </div>
                            </div>
                        )}

                        {!isLoading && !currentLesson && (
                            <div className="flex h-full items-center justify-center text-muted-foreground">
                                {LESSON_PLAYER.LESSON_NOT_FOUND}
                            </div>
                        )}

                        {!isLoading && currentLesson && (
                            <>
                                {currentLesson.lessonType === 'Video' && (
                                    <VideoLessonView lesson={currentLesson} courseId={courseId!} />
                                )}
                                {currentLesson.lessonType === 'Post' && (
                                    <PostLessonView lesson={currentLesson} courseId={courseId!} />
                                )}
                                {currentLesson.lessonType === 'Test' && (
                                    <TestLessonPreview
                                        lesson={currentLesson}
                                        courseId={courseId!}
                                    />
                                )}
                            </>
                        )}
                    </main>

                    {/* Bottom navigation bar */}
                    <div className="flex shrink-0 items-center justify-between border-t border-border bg-card px-6 py-3">
                        {/* Prev */}
                        <div className="w-32">
                            {prevLesson && (
                                <Link
                                    to={`/courses/${courseId}/learn/${prevLesson.lessonId}`}
                                    className="inline-flex items-center gap-1.5 rounded-lg px-3 py-2 text-sm font-medium text-muted-foreground transition-colors hover:bg-secondary hover:text-foreground"
                                >
                                    <ChevronLeft className="h-4 w-4" />
                                    {LESSON_PLAYER.ACTIONS.previousLesson}
                                </Link>
                            )}
                        </div>

                        {/* Mark complete — hidden for test lessons (completed via submission) */}
                        {currentLesson && currentLesson.lessonType !== 'Test' && (
                            <button
                                type="button"
                                onClick={handleMarkComplete}
                                disabled={currentLesson.isCompleted || markComplete.isPending}
                                className={cn(
                                    'inline-flex items-center gap-2 rounded-lg px-5 py-2 text-sm font-medium transition-colors',
                                    currentLesson.isCompleted
                                        ? 'cursor-default bg-success/15 text-success'
                                        : 'bg-primary text-primary-foreground hover:bg-primary/90 disabled:opacity-60',
                                )}
                            >
                                {currentLesson.isCompleted && <CheckCircle2 className="h-4 w-4" />}
                                {currentLesson.isCompleted
                                    ? LESSON_PLAYER.ACTIONS.completed
                                    : markComplete.isPending
                                      ? 'Saving...'
                                      : LESSON_PLAYER.ACTIONS.markComplete}
                            </button>
                        )}
                        {/* For test lessons, show completion badge only */}
                        {currentLesson &&
                            currentLesson.lessonType === 'Test' &&
                            currentLesson.isCompleted && (
                                <span className="inline-flex items-center gap-2 rounded-lg bg-success/15 px-5 py-2 text-sm font-medium text-success">
                                    <CheckCircle2 className="h-4 w-4" />
                                    {LESSON_PLAYER.ACTIONS.completed}
                                </span>
                            )}

                        {/* Next */}
                        <div className="w-32 text-right">
                            {nextLesson && (
                                <Link
                                    to={`/courses/${courseId}/learn/${nextLesson.lessonId}`}
                                    className="inline-flex items-center gap-1.5 rounded-lg px-3 py-2 text-sm font-medium text-muted-foreground transition-colors hover:bg-secondary hover:text-foreground"
                                >
                                    {LESSON_PLAYER.ACTIONS.nextLesson}
                                    <ChevronRight className="h-4 w-4" />
                                </Link>
                            )}
                        </div>
                    </div>
                </div>
            </div>
        </div>
    );
}
