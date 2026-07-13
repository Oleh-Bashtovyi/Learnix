import { useEffect, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { useMutation } from '@tanstack/react-query';
import {
    ArrowLeft,
    CheckCircle2,
    ChevronLeft,
    ChevronRight,
    Menu,
    MessageSquare,
    PanelLeftOpen,
    Sparkles,
} from 'lucide-react';
import { messagesApi } from '@/api/messages.api';
import { CourseCertificateButton } from '@/components/common/course/CourseCertificateButton';
import { BrandLogo } from '@/components/common/ui/BrandLogo';
import { LanguageSwitcher } from '@/components/common/ui/LanguageSwitcher';
import { ThemeSwitcher } from '@/components/common/ui/ThemeSwitcher';
import { AsyncButton } from '@/components/ui/async-button';
import { ResizableHandle, ResizablePanel, ResizablePanelGroup } from '@/components/ui/resizable';
import { useCourseDetail } from '@/hooks/course/useCourseDetail';
import { useCourseProgress } from '@/hooks/lesson/useCourseProgress';
import { useMarkLessonComplete } from '@/hooks/lesson/useMarkLessonComplete';
import { useAiChat } from '@/hooks/realtime/useAiChat';
import { useMediaQuery } from '@/hooks/shared/useMediaQuery';
import { APP_ROUTES } from '@/routes/paths';
import { ONBOARDING_HINTS, useOnboardingStore } from '@/store/onboarding.store';
import type { ChatScope } from '@/types/aiChat.types';
import type { ConversationSummary } from '@/types/message.types';
import { cn } from '@/utils/cn';
import { AssistantHint } from './components/AssistantHint';
import { AssistantPanel, type AssistantTab } from './components/AssistantPanel';
import { CourseCertificateDropdown } from './components/CourseCertificateDropdown';
import { CourseSidebar } from './components/CourseSidebar';
import { PostLessonView } from './components/PostLessonView';
import { TestLessonPreview } from './components/TestLessonPreview';
import { VideoLessonView } from './components/VideoLessonView';

// react-resizable-panels builds its initial layout by summing every panel's `defaultSize`
// and only normalises panels that leave it undefined — so each configuration must total 100.
const SIDEBAR_SIZE = '20';
const MAIN_SIZE = '80';
const ASSISTANT_SIZE = '28';
const MAIN_SIZE_WITH_ASSISTANT = '52';

export default function CoursePlayerPage() {
    const { courseId, lessonId } = useParams<{ courseId: string; lessonId: string }>();
    const navigate = useNavigate();
    const { t } = useTranslation('lessonPlayer');
    const { data: course } = useCourseDetail(courseId!);
    const { data: progress, isLoading } = useCourseProgress(courseId!);
    const markComplete = useMarkLessonComplete(courseId!);
    const isDesktop = useMediaQuery('(min-width: 1024px)');
    const [isSidebarOpen, setIsSidebarOpen] = useState(false);
    const [isSidebarCollapsed, setIsSidebarCollapsed] = useState(false);
    const [assistantTab, setAssistantTab] = useState<AssistantTab | null>(null);

    // Shown once, ever — the flag lives in localStorage with no expiry (onboarding.store).
    const dismissedHints = useOnboardingStore((s) => s.dismissed);
    const dismissHint = useOnboardingStore((s) => s.dismiss);
    const showAssistantHint =
        !dismissedHints[ONBOARDING_HINTS.coursePlayerAssistant] && assistantTab === null;
    const [hasOpenedAiChat, setHasOpenedAiChat] = useState(false);
    const [activeChat, setActiveChat] = useState<ConversationSummary | null>(null);
    const [prevLessonId, setPrevLessonId] = useState(lessonId);

    const chatScope = useMemo<ChatScope>(
        () => ({ kind: 'course', courseId: courseId! }),
        [courseId],
    );

    // Owned here rather than inside the panel, so closing the panel does not
    // abort an in-flight stream or discard the message list.
    const aiChat = useAiChat(hasOpenedAiChat, chatScope, lessonId);

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
                    navigate(APP_ROUTES.student.learnLesson(courseId!, nextLesson.lessonId));
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
            navigate(APP_ROUTES.student.learnLesson(courseId!, nextLesson.lessonId));
        }
    };

    // The tab strip only ever switches. The header buttons double as an open/close toggle,
    // the way an activity bar does — clicking the tab you are already on dismisses the panel.
    const selectAssistantTab = (tab: AssistantTab) => {
        setAssistantTab(tab);

        if (tab === 'ai') {
            setHasOpenedAiChat(true);
        } else if (!activeChat && !startChat.isPending) {
            startChat.mutate();
        }
    };

    const toggleAssistant = (tab: AssistantTab) => {
        // Opening either panel is the discovery the hint exists to cause. Once it has happened, saying
        // it again on the next lesson would be telling somebody what they just did.
        dismissHint(ONBOARDING_HINTS.coursePlayerAssistant);

        if (assistantTab === tab) {
            setAssistantTab(null);
            return;
        }
        selectAssistantTab(tab);
    };

    const assistantElement = assistantTab && (
        <AssistantPanel
            activeTab={assistantTab}
            onTabChange={selectAssistantTab}
            onClose={() => setAssistantTab(null)}
            chat={aiChat}
            conversation={activeChat}
            isConversationLoading={startChat.isPending}
            lessonTitle={currentLesson?.title}
            isFullScreen={!isDesktop}
        />
    );

    const sidebarElement = (
        <CourseSidebar
            sections={progress?.sections ?? []}
            currentLessonId={lessonId!}
            courseId={courseId!}
            onClose={() => (isDesktop ? setIsSidebarCollapsed(true) : setIsSidebarOpen(false))}
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

    // h-dvh, not h-screen: on a phone 100vh excludes the browser's URL bar, so the shell overflows the
    // visible area and the lesson footer (mark-complete, prev/next) sits below the fold until scrolled to.
    return (
        <div className="flex h-dvh flex-col overflow-hidden bg-background">
            {/* Top bar */}
            <header className="flex h-14 shrink-0 items-center justify-between border-b border-border bg-card px-4">
                <div className="flex min-w-0 items-center gap-3">
                    {/* Opens the overlay on mobile; on desktop it is the way back from a collapsed sidebar */}
                    {/* Mobile only: on desktop the sidebar is re-opened from the content edge, far
                        away from the "my learning" link — a double click there must not navigate. */}
                    <button
                        type="button"
                        onClick={() => setIsSidebarOpen(true)}
                        title={t('sidebar.courseContent')}
                        className="mr-1 rounded-md p-1.5 text-muted-foreground hover:bg-secondary hover:text-foreground lg:hidden"
                    >
                        <Menu className="size-5" />
                    </button>
                    <Link
                        to={APP_ROUTES.student.myLearning}
                        title={t('header.myLearning')}
                        className="hidden shrink-0 items-center gap-1.5 rounded-md p-1.5 pr-2 text-sm text-muted-foreground transition-colors hover:bg-secondary hover:text-foreground lg:flex"
                    >
                        <ArrowLeft className="size-4" />
                        <span>{t('header.myLearning')}</span>
                    </Link>
                    <span className="hidden text-border lg:inline">|</span>
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
                    {/* The hint hangs off these two buttons, not off the whole header group. The theme
                        and language switchers to their right disappear below `lg:`, so an arrow measured
                        from the group's edge pointed somewhere different at every width. Anchored here it
                        lands on the same button whatever else is on screen. */}
                    <div className="relative flex items-center gap-2">
                        {showAssistantHint && (
                            <AssistantHint
                                onDismiss={() =>
                                    dismissHint(ONBOARDING_HINTS.coursePlayerAssistant)
                                }
                            />
                        )}

                        <button
                            type="button"
                            onClick={() => toggleAssistant('ai')}
                            aria-pressed={assistantTab === 'ai'}
                            title={t('header.aiAssistant')}
                            className={cn(
                                'grid size-8 place-items-center rounded-lg transition-colors hover:bg-secondary hover:text-foreground',
                                assistantTab === 'ai'
                                    ? 'bg-secondary text-primary'
                                    : 'text-muted-foreground',
                            )}
                        >
                            <Sparkles className="size-4" />
                        </button>
                        <button
                            type="button"
                            onClick={() => toggleAssistant('instructor')}
                            aria-pressed={assistantTab === 'instructor'}
                            title={t('header.messageInstructor')}
                            className={cn(
                                'grid size-8 place-items-center rounded-lg transition-colors hover:bg-secondary hover:text-foreground',
                                assistantTab === 'instructor'
                                    ? 'bg-secondary text-primary'
                                    : 'text-muted-foreground',
                            )}
                        >
                            <MessageSquare className="size-4" />
                        </button>
                    </div>
                    <div className="hidden items-center gap-1 lg:flex">
                        <ThemeSwitcher />
                        <LanguageSwitcher />
                    </div>
                </div>
            </header>

            {/* Content */}
            <div className="relative flex min-h-0 flex-1 overflow-hidden">
                {isDesktop && isSidebarCollapsed && (
                    <button
                        type="button"
                        onClick={() => setIsSidebarCollapsed(false)}
                        title={t('sidebar.courseContent')}
                        className="absolute left-0 top-4 z-10 rounded-r-lg border border-l-0 border-border bg-card py-3 pl-2.5 pr-3 text-muted-foreground shadow-md transition-colors hover:bg-secondary hover:text-foreground"
                    >
                        <span className="sr-only">{t('sidebar.courseContent')}</span>
                        <PanelLeftOpen className="size-5" />
                    </button>
                )}

                {isDesktop ? (
                    <ResizablePanelGroup direction="horizontal" className="size-full">
                        {!isSidebarCollapsed && (
                            <>
                                <ResizablePanel
                                    id="course-sidebar"
                                    defaultSize={SIDEBAR_SIZE}
                                    minSize="15"
                                    maxSize="30"
                                    className="flex min-w-0 flex-col overflow-hidden bg-card"
                                >
                                    {sidebarElement}
                                </ResizablePanel>

                                <ResizableHandle withHandle />
                            </>
                        )}

                        <ResizablePanel
                            id="lesson-main"
                            defaultSize={assistantTab ? MAIN_SIZE_WITH_ASSISTANT : MAIN_SIZE}
                            className="flex min-w-0 flex-col overflow-hidden bg-background"
                        >
                            {mainElement}
                        </ResizablePanel>

                        {assistantElement && (
                            <>
                                <ResizableHandle withHandle />
                                <ResizablePanel
                                    id="assistant"
                                    defaultSize={ASSISTANT_SIZE}
                                    minSize="22"
                                    maxSize="45"
                                    className="flex min-w-0 flex-col overflow-hidden bg-card"
                                >
                                    {assistantElement}
                                </ResizablePanel>
                            </>
                        )}
                    </ResizablePanelGroup>
                ) : (
                    <div className="flex size-full flex-col overflow-hidden">
                        {/* Mobile sidebar overlay */}
                        {isSidebarOpen && (
                            <button
                                type="button"
                                aria-label={t('common:actions.close')}
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
                )}
            </div>

            {/* Mobile: the assistant takes over the screen — there is no room to dock it */}
            {!isDesktop && assistantElement && (
                <div className="fixed inset-0 z-[70] flex flex-col">{assistantElement}</div>
            )}
        </div>
    );
}
