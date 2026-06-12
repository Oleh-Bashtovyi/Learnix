import { useMemo, useEffect, useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { useMutation } from '@tanstack/react-query';
import { MessageSquare, ChevronLeft, ChevronRight, CheckCircle2, Menu, X } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { cn } from '@/utils/cn';
import { messagesApi } from '@/api/messages.api';
import { useCourseDetail } from '@/hooks/useCourseDetail';
import { useCourseProgress } from '@/hooks/useCourseProgress';
import { useMarkLessonComplete } from '@/hooks/useMarkLessonComplete';
import { CourseSidebar } from './components/CourseSidebar';
import { VideoLessonView } from './components/VideoLessonView';
import { PostLessonView } from './components/PostLessonView';
import { TestLessonPreview } from './components/TestLessonPreview';
import { CourseCertificateButton } from '@/components/common/CourseCertificateButton';
import { CourseCertificateDropdown } from './components/CourseCertificateDropdown';
import { Logo } from '@/components/common/Logo';

export default function CoursePlayerPage() {
    const { courseId, lessonId } = useParams<{ courseId: string; lessonId: string }>();
    const navigate = useNavigate();
    const { t } = useTranslation('lessonPlayer');

    const { data: course } = useCourseDetail(courseId!);
    const { data: progress, isLoading } = useCourseProgress(courseId!);
    const markComplete = useMarkLessonComplete(courseId!);
    const [isSidebarOpen, setIsSidebarOpen] = useState(false);

    const startChat = useMutation({
        mutationFn: () => messagesApi.startOrGet({ courseId: courseId! }),
        onSuccess: (conversation) => {
            navigate('/messages', { state: { initialConversation: conversation } });
        },
    });

    useEffect(() => {
        if (courseId && lessonId) {
            localStorage.setItem(`lastLesson_${courseId}`, lessonId);
            // Close sidebar automatically when navigating to a new lesson on mobile
            setIsSidebarOpen(false);
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
                    <button
                        type="button"
                        onClick={() => setIsSidebarOpen(true)}
                        className="mr-1 rounded-md p-1.5 text-muted-foreground hover:bg-secondary hover:text-foreground lg:hidden"
                    >
                        <Menu className="h-5 w-5" />
                    </button>
                    <Link
                        to="/"
                        className="flex shrink-0 items-center gap-2 font-heading font-bold"
                    >
                        <Logo className="h-7 w-7 text-primary" />
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
            <div className="relative flex min-h-0 flex-1">
                {/* Mobile sidebar overlay */}
                {isSidebarOpen && (
                    <div
                        className="fixed inset-0 z-40 bg-background/80 backdrop-blur-sm lg:hidden"
                        onClick={() => setIsSidebarOpen(false)}
                    />
                )}

                {/* Sidebar */}
                <div
                    className={cn(
                        'fixed inset-y-0 left-0 z-50 transform bg-card transition-transform duration-300 lg:static lg:z-0 lg:translate-x-0',
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
                </div>

                {/* Main lesson area */}
                <div className="flex min-w-0 flex-1 flex-col overflow-hidden">
                    <main className="flex-1 overflow-y-auto p-8">
                        {isLoading && (
                            <div className="flex h-full items-center justify-center">
                                <div className="space-y-3 text-center">
                                    <div className="mx-auto h-10 w-10 animate-spin rounded-full border-2 border-primary border-t-transparent" />
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
                                        lesson={currentLesson}
                                        courseId={courseId!}
                                        onVideoEnded={handleMarkComplete}
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
                                    to={`/courses/${courseId}/learn/${prevLesson.lessonId}`}
                                    className="inline-flex items-center gap-1.5 rounded-lg px-3 py-2 text-sm font-medium text-muted-foreground transition-colors hover:bg-secondary hover:text-foreground"
                                >
                                    <ChevronLeft className="h-4 w-4" />
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
                                        <CheckCircle2 className="h-4 w-4" />
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
                                        <CheckCircle2 className="h-4 w-4" />
                                        {t('actions.completed')}
                                    </span>
                                )}
                        </div>

                        {/* Next or Certificate */}
                        <div className="order-2 flex w-auto justify-end sm:order-3 sm:w-32">
                            {nextLesson ? (
                                <Link
                                    to={`/courses/${courseId}/learn/${nextLesson.lessonId}`}
                                    className="inline-flex items-center gap-1.5 rounded-lg px-3 py-2 text-sm font-medium text-muted-foreground transition-colors hover:bg-secondary hover:text-foreground"
                                >
                                    <span className="hidden sm:inline">
                                        {t('actions.nextLesson')}
                                    </span>
                                    <span className="sm:hidden">Next</span>
                                    <ChevronRight className="h-4 w-4" />
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
                </div>
            </div>
        </div>
    );
}
