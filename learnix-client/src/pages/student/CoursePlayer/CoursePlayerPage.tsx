import { useEffect, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { useMutation } from '@tanstack/react-query';
import { CheckCircle2, ChevronLeft, ChevronRight, Menu, MessageSquare, X } from 'lucide-react';
import { messagesApi } from '@/api/messages.api';
import { CourseCertificateButton } from '@/components/common/course/CourseCertificateButton';
import { ConversationView } from '@/components/common/messaging/ConversationView';
import { BrandLogo } from '@/components/common/ui/BrandLogo';
import { LanguageSwitcher } from '@/components/common/ui/LanguageSwitcher';
import { ThemeSwitcher } from '@/components/common/ui/ThemeSwitcher';
import { AsyncButton } from '@/components/ui/async-button';
import { ResizableHandle, ResizablePanel, ResizablePanelGroup } from '@/components/ui/resizable';
import { useCourseDetail } from '@/hooks/course/useCourseDetail';
import { useCourseProgress } from '@/hooks/lesson/useCourseProgress';
import { useMarkLessonComplete } from '@/hooks/lesson/useMarkLessonComplete';
import { APP_ROUTES } from '@/routes/paths';
import type { ConversationSummary } from '@/types/message.types';
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
    const [activeChat, setActiveChat] = useState<ConversationSummary | null>(null);
    const [prevLessonId, setPrevLessonId] = useState(lessonId);

    if (lessonId !== prevLessonId) {
        setPrevLessonId(lessonId);
        setIsSidebarOpen(false);
    }

    const startChat = useMutation({
        mutationFn: () => messagesApi.startOrGet({ courseId: courseId! }),
        onSuccess: (conversation) => {
            setActiveChat(conversation as unknown as ConversationSummary);
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

    const sidebarElement = (
        <CourseSidebar
            sections={progress?.sections ?? []}
            currentLessonId={lessonId!}
            courseId={courseId!}
            totalLessons={progress?.totalLessons ?? 0}
            completedLessons={progress?.completedLessons ?? 0}
            onCloseMobile={() => setIsSidebarOpen(false)}
        />
    );

    const mainElement = (
        <>
            <main className="flex-1 overflow-y-auto px-4 py-6 md:p-8">
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
                            <TestLessonPreview lesson={currentLesson} courseId={courseId!} />
                        )}
                    </>
                )}
            </main>

            {/* Bottom navigation bar */}
            <div className="flex shrink-0 items-center justify-between gap-2 border-t border-border bg-card p-3 sm:p-4 sm:px-6">
                {/* Prev */}
                <div className="flex flex-1 justify-start">
                    {prevLesson && (
                        <Link
                            to={APP_ROUTES.student.learnLesson(courseId!, prevLesson.lessonId)}
                            className="inline-flex items-center gap-1.5 rounded-lg p-3 text-sm font-medium text-muted-foreground transition-colors hover:bg-secondary hover:text-foreground sm:px-4"
                            title={t('common:actions.previous')}
                        >
                            <ChevronLeft className="size-5" />
                            <span className="hidden sm:inline">{t('common:actions.previous')}</span>
                        </Link>
                    )}
                </div>

                {/* Center Action Button */}
                <div className="flex shrink-0 justify-center">
                    {currentLesson && currentLesson.lessonType !== 'Test' && (
                        <AsyncButton
                            type="button"
                            onClick={handleMarkComplete}
                            disabled={currentLesson.isCompleted || markComplete.isPending}
                            isLoading={markComplete.isPending}
                            loadingText={t('common:status.saving', 'Saving...')}
                            className={cn(
                                'inline-flex h-auto items-center justify-center gap-2 rounded-lg px-6 py-3 text-sm font-medium transition-colors',
                                currentLesson.isCompleted
                                    ? 'cursor-default bg-success/15 text-success hover:bg-success/15 hover:text-success'
                                    : 'bg-primary text-primary-foreground hover:bg-primary/90 disabled:opacity-60',
                            )}
                        >
                            {currentLesson.isCompleted && <CheckCircle2 className="size-5" />}
                            {currentLesson.isCompleted
                                ? t('common:status.completed')
                                : t('actions.markComplete')}
                        </AsyncButton>
                    )}
                    {/* For test lessons, show completion badge only */}
                    {currentLesson &&
                        currentLesson.lessonType === 'Test' &&
                        currentLesson.isCompleted && (
                            <span className="inline-flex items-center justify-center gap-2 rounded-lg bg-success/15 px-6 py-3 text-sm font-medium text-success">
                                <CheckCircle2 className="size-5" />
                                {t('common:status.completed')}
                            </span>
                        )}
                </div>

                {/* Next */}
                <div className="flex flex-1 justify-end">
                    {nextLesson ? (
                        <Link
                            to={APP_ROUTES.student.learnLesson(courseId!, nextLesson.lessonId)}
                            className="inline-flex items-center gap-1.5 rounded-lg p-3 text-sm font-medium text-muted-foreground transition-colors hover:bg-secondary hover:text-foreground sm:px-4"
                            title={t('common:actions.next')}
                        >
                            <span className="hidden sm:inline">{t('common:actions.next')}</span>
                            <ChevronRight className="size-5" />
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
        </>
    );

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
                    <BrandLogo
                        boxClassName="size-7"
                        iconClassName="size-5"
                        textClassName="hidden text-sm sm:block"
                    />
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
                    <div className="hidden items-center gap-1 lg:flex">
                        <LanguageSwitcher />
                        <ThemeSwitcher />
                    </div>
                </div>
            </header>

            {/* Content */}
            <div className="relative flex min-h-0 flex-1 overflow-hidden">
                {/* MOBILE LAYOUT */}
                <div className="flex size-full flex-col overflow-hidden lg:hidden">
                    {/* Mobile sidebar overlay */}
                    {isSidebarOpen && (
                        <div
                            className="fixed inset-0 z-40 bg-background/80 backdrop-blur-sm"
                            onClick={() => setIsSidebarOpen(false)}
                        />
                    )}

                    {/* Mobile sidebar drawer */}
                    <div
                        className={cn(
                            'fixed inset-y-0 left-0 z-50 flex w-72 transform flex-col bg-card shadow-2xl transition-transform duration-300',
                            isSidebarOpen ? 'translate-x-0' : '-translate-x-full',
                        )}
                    >
                        {sidebarElement}
                    </div>

                    {/* Mobile main content */}
                    <div className="flex min-w-0 flex-1 flex-col overflow-hidden bg-background">
                        {mainElement}
                    </div>
                </div>

                {/* DESKTOP LAYOUT (Resizable) */}
                <div className="hidden size-full overflow-hidden lg:flex">
                    <ResizablePanelGroup direction="horizontal" className="size-full">
                        <ResizablePanel
                            defaultSize="20"
                            minSize="15"
                            maxSize="30"
                            className="flex min-w-0 flex-col overflow-hidden bg-card"
                        >
                            {sidebarElement}
                        </ResizablePanel>

                        <ResizableHandle withHandle />

                        <ResizablePanel
                            defaultSize="80"
                            className="flex min-w-0 flex-col overflow-hidden bg-background"
                        >
                            {mainElement}
                        </ResizablePanel>
                    </ResizablePanelGroup>
                </div>
            </div>
            {/* Chat Slide-over Overlay */}
            {activeChat && (
                <>
                    {/* Backdrop */}
                    <div
                        className="fixed inset-0 z-[60] bg-background/80 backdrop-blur-sm"
                        onClick={() => setActiveChat(null)}
                    />

                    {/* Drawer Panel */}
                    <div className="fixed inset-y-0 right-0 z-[70] flex w-full max-w-md translate-x-0 transform flex-col bg-card shadow-2xl transition-transform duration-300">
                        <div className="relative flex h-full flex-col">
                            {/* Desktop Close Button (Mobile uses the back button in ConversationView) */}
                            <button
                                onClick={() => setActiveChat(null)}
                                className="absolute right-4 top-3 z-10 hidden rounded-md p-2 text-muted-foreground transition-colors hover:bg-secondary hover:text-foreground md:block"
                            >
                                <X className="size-5" />
                                <span className="sr-only">Close chat</span>
                            </button>

                            <ConversationView
                                conversation={activeChat}
                                onBack={() => setActiveChat(null)}
                            />
                        </div>
                    </div>
                </>
            )}
        </div>
    );
}
