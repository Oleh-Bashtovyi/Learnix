import { useEffect, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { useMutation } from '@tanstack/react-query';
import { CheckCircle2, ChevronLeft, ChevronRight, Menu, MessageSquare } from 'lucide-react';
import { messagesApi } from '@/api/messages.api';
import { CourseCertificateButton } from '@/components/common/course/CourseCertificateButton';
import { Logo } from '@/components/common/ui/Logo';
import { ResizableHandle, ResizablePanel, ResizablePanelGroup } from '@/components/ui/resizable';
import { useCourseDetail } from '@/hooks/course/useCourseDetail';
import { useCourseProgress } from '@/hooks/lesson/useCourseProgress';
import { useMarkLessonComplete } from '@/hooks/lesson/useMarkLessonComplete';
import { APP_ROUTES } from '@/routes/paths';
import { cn } from '@/utils/cn';
import { CourseCertificateDropdown } from './components/CourseCertificateDropdown';
import { CourseSidebar } from './components/CourseSidebar';
import { PostLessonView } from './components/PostLessonView';
import { TestLessonPreview } from './components/TestLessonPreview';
import { VideoLessonView } from './components/VideoLessonView';

export default function CoursePlayerPage() {
    const { courseId, lessonId } = useParams<{ courseId: string; lessonId: string }>();
    const navigate = useNavigate();
    const { t } = useTranslation('lessonPlayer');

    const { data: course } = useCourseDetail(courseId!);
    const { data: progress, isLoading } = useCourseProgress(courseId!);
    const markComplete = useMarkLessonComplete(courseId!);
    const [isSidebarOpen, setIsSidebarOpen] = useState(false);
    const [prevLessonId, setPrevLessonId] = useState(lessonId);

    if (lessonId !== prevLessonId) {
        setPrevLessonId(lessonId);
        setIsSidebarOpen(false);
    }

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

    // Called when video crosses the near-end threshold: mark as done, but do NOT redirect.
    const handleAutoMarkComplete = () => {
        if (!lessonId || currentLesson?.isCompleted) return;
        markComplete.mutate(lessonId);
    };

    // Called only when the video reaches its true end: navigate to the next lesson.
    const handleVideoFullyEnded = () => {
        if (nextLesson) {
            navigate(`/courses/${courseId}/learn/${nextLesson.lessonId}`);
        }
    };

    return (
        <div className="flex h-screen flex-col overflow-hidden bg-background">
            {/* Top bar */}
            <header className="flex h-14 shrink-0 items-center justify-between border-b border-border bg-card px-4">
                <div className="flex min-w-0 items-center gap-3">
                    <button
                        type="button"
                        onClick={() => setIsSidebarOpen(true)}
                        className="mr-1 rounded-md p-1.5 text-muted-foreground hover:bg-secondary hover:text-foreground lg:hidden"
                    >
                        <Menu className="size-5" />
                    </button>
                    <Link
                        to={APP_ROUTES.public.home}
                        className="flex shrink-0 items-center gap-2 font-heading font-bold"
                    >
                        <Logo className="size-7 text-primary" />
                        <span className="hidden text-sm sm:block">Learnix</span>
                    </Link>
                    {course && (
                        <>
                            <span className="text-border">|</span>
                            <Link
                                to={APP_ROUTES.public.courseDetail(courseId!)}
                                className="truncate text-sm font-medium text-foreground transition-colors hover:text-primary"
                            >
                                {course.title}
                            </Link>
                        </>
                    )}
                </div>

                <div className="flex shrink-0 items-center gap-2">
                    {course && (
                        <div className="mr-1 hidden border-r border-border pr-2 sm:block">
                            <CourseCertificateDropdown
                                courseId={courseId!}
                                completedLessons={progress?.completedLessons ?? 0}
                                totalLessons={progress?.totalLessons ?? 0}
                            />
                        </div>
                    )}
                    <button
                        type="button"
                        onClick={() => startChat.mutate()}
                        disabled={startChat.isPending}
                        title={t('header.messageInstructor')}
                        className="grid size-8 place-items-center rounded-lg text-muted-foreground transition-colors hover:bg-secondary hover:text-foreground disabled:opacity-50"
                    >
                        {startChat.isPending ? (
                            <div className="size-4 animate-spin rounded-full border-2 border-current border-t-transparent" />
                        ) : (
                            <MessageSquare className="size-4" />
                        )}
                    </button>
                </div>
            </header>

            {/* Content */}
            <ResizablePanelGroup direction="horizontal" className="relative flex min-h-0 flex-1">
                {/* Mobile sidebar overlay */}
                {isSidebarOpen && (
                    <div
                        className="fixed inset-0 z-40 bg-background/80 backdrop-blur-sm lg:hidden"
                        onClick={() => setIsSidebarOpen(false)}
                    />
                )}

                {/* Sidebar */}
                <ResizablePanel
                    defaultSize={'20'}
                    minSize={'15'}
                    maxSize={'30'}
                    className={cn(
                        'fixed inset-y-0 left-0 z-50 transform bg-card transition-transform duration-300 lg:static lg:z-0 lg:translate-x-0 lg:!transform-none',
                        isSidebarOpen ? 'translate-x-0' : '-translate-x-full',
                    )}
                >
                    <CourseSidebar
                        sections={progress?.sections ?? []}
                        currentLessonId={lessonId!}
                        courseId={courseId!}
                        totalLessons={progress?.totalLessons ?? 0}
                        completedLessons={progress?.completedLessons ?? 0}
                        onCloseMobile={() => setIsSidebarOpen(false)}
                    />
                </ResizablePanel>

                <ResizableHandle withHandle className="hidden lg:flex" />

                {/* Main lesson area */}
                <ResizablePanel
                    defaultSize={'80'}
                    className="flex min-w-0 flex-1 flex-col overflow-hidden"
                >
                    <main className="flex-1 overflow-y-auto p-8">
                        {isLoading && (
                            <div className="flex h-full items-center justify-center">
                                <div className="space-y-3 text-center">
                                    <div className="mx-auto size-10 animate-spin rounded-full border-2 border-primary border-t-transparent" />
                                    <p className="text-sm text-muted-foreground">{t('loading')}</p>
                                </div>
                            </div>
                        )}

                        {!isLoading && !currentLesson && (
                            <div className="flex h-full items-center justify-center text-muted-foreground">
                                {t('lessonNotFound')}
                            </div>
                        )}

                        {!isLoading && currentLesson && (
                            <>
                                {currentLesson.lessonType === 'Video' && (
                                    <VideoLessonView
                                        key={currentLesson.lessonId}
                                        lesson={currentLesson}
                                        courseId={courseId!}
                                        nextLessonTitle={nextLesson?.title}
                                        onVideoNearEnd={handleAutoMarkComplete}
                                        onPlayNext={handleVideoFullyEnded}
                                    />
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
                    <div className="flex shrink-0 flex-wrap items-center justify-between gap-3 border-t border-border bg-card px-4 py-3 sm:px-6">
                        {/* Prev */}
                        <div className="order-1 w-auto sm:w-32">
                            {prevLesson && (
                                <Link
                                    to={APP_ROUTES.student.learnLesson(
                                        courseId!,
                                        prevLesson.lessonId,
                                    )}
                                    className="inline-flex items-center gap-1.5 rounded-lg px-3 py-2 text-sm font-medium text-muted-foreground transition-colors hover:bg-secondary hover:text-foreground"
                                >
                                    <ChevronLeft className="size-4" />
                                    <span className="hidden sm:inline">
                                        {t('actions.previousLesson')}
                                    </span>
                                    <span className="sm:hidden">Prev</span>
                                </Link>
                            )}
                        </div>

                        {/* Mark complete — hidden for test lessons (completed via submission) */}
                        <div className="order-3 mt-2 flex w-full flex-1 justify-center sm:order-2 sm:mt-0 sm:w-auto sm:flex-none">
                            {currentLesson && currentLesson.lessonType !== 'Test' && (
                                <button
                                    type="button"
                                    onClick={handleMarkComplete}
                                    disabled={currentLesson.isCompleted || markComplete.isPending}
                                    className={cn(
                                        'inline-flex w-full items-center justify-center gap-2 rounded-lg px-5 py-2.5 text-sm font-medium transition-colors sm:w-auto sm:py-2',
                                        currentLesson.isCompleted
                                            ? 'cursor-default bg-success/15 text-success'
                                            : 'bg-primary text-primary-foreground hover:bg-primary/90 disabled:opacity-60',
                                    )}
                                >
                                    {currentLesson.isCompleted && (
                                        <CheckCircle2 className="size-4" />
                                    )}
                                    {currentLesson.isCompleted
                                        ? t('actions.completed')
                                        : markComplete.isPending
                                          ? 'Saving...'
                                          : t('actions.markComplete')}
                                </button>
                            )}
                            {/* For test lessons, show completion badge only */}
                            {currentLesson &&
                                currentLesson.lessonType === 'Test' &&
                                currentLesson.isCompleted && (
                                    <span className="inline-flex w-full items-center justify-center gap-2 rounded-lg bg-success/15 px-5 py-2.5 text-sm font-medium text-success sm:w-auto sm:py-2">
                                        <CheckCircle2 className="size-4" />
                                        {t('actions.completed')}
                                    </span>
                                )}
                        </div>

                        {/* Next or Certificate */}
                        <div className="order-2 flex w-auto justify-end sm:order-3 sm:w-32">
                            {nextLesson ? (
                                <Link
                                    to={APP_ROUTES.student.learnLesson(
                                        courseId!,
                                        nextLesson.lessonId,
                                    )}
                                    className="inline-flex items-center gap-1.5 rounded-lg px-3 py-2 text-sm font-medium text-muted-foreground transition-colors hover:bg-secondary hover:text-foreground"
                                >
                                    <span className="hidden sm:inline">
                                        {t('actions.nextLesson')}
                                    </span>
                                    <span className="sm:hidden">Next</span>
                                    <ChevronRight className="size-4" />
                                </Link>
                            ) : progress?.completedLessons === progress?.totalLessons ? (
                                <CourseCertificateButton
                                    courseId={courseId!}
                                    variant="primary"
                                    showIconOnlyOnMobile
                                />
                            ) : null}
                        </div>
                    </div>
                </ResizablePanel>
            </ResizablePanelGroup>
        </div>
    );
}
