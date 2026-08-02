import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import {
    ArrowLeft,
    CheckCircle2,
    ChevronDown,
    ClipboardList,
    FileText,
    PlayCircle,
    X,
} from 'lucide-react';
import { LanguageSwitcher } from '@/components/common/elements/LanguageSwitcher';
import { ThemeSwitcher } from '@/components/common/elements/ThemeSwitcher';
import { useFormatDuration } from '@/hooks/shared/useFormatDuration';
import { APP_ROUTES } from '@/routes/paths';
import type { LessonProgressItemDto, SectionProgressDto } from '@/types/progress.types';
import { cn } from '@/utils/cn';

interface CourseSidebarProps {
    sections: SectionProgressDto[];
    currentLessonId: string;
    courseId: string;
    /** Closes the overlay on mobile, collapses the panel on desktop. */
    onClose?: () => void;
}

const lessonTypeIcon = {
    Video: PlayCircle,
    Post: FileText,
    Test: ClipboardList,
};

export function CourseSidebar({
    sections,
    currentLessonId,
    courseId,
    onClose,
}: CourseSidebarProps) {
    const { t } = useTranslation('lessonPlayer');
    const formatDuration = useFormatDuration();

    /**
     * What sits under a lesson title: a length for video and post, a question count for a test.
     * Loose `!= null` on purpose — an older API omits these fields entirely, and `undefined !== null`
     * would send every lesson down the question-count branch.
     */
    function formatMeta(lesson: LessonProgressItemDto) {
        if (lesson.questionCount != null) {
            return t('common:lessonMeta.questionsCount', { count: lesson.questionCount });
        }

        return lesson.durationSeconds != null ? formatDuration(lesson.durationSeconds) : null;
    }

    const activeSectionId = sections.find((s) =>
        s.lessons.some((l) => l.lessonId === currentLessonId),
    )?.sectionId;

    const [openSections, setOpenSections] = useState<Set<string>>(
        () => new Set(activeSectionId ? [activeSectionId] : []),
    );
    const [prevActiveSectionId, setPrevActiveSectionId] = useState(activeSectionId);

    if (activeSectionId !== prevActiveSectionId) {
        setPrevActiveSectionId(activeSectionId);
        if (activeSectionId && !openSections.has(activeSectionId)) {
            setOpenSections((prev) => new Set([...prev, activeSectionId]));
        }
    }

    function toggleSection(id: string) {
        setOpenSections((prev) => {
            const next = new Set(prev);
            if (next.has(id)) {
                next.delete(id);
            } else {
                next.add(id);
            }
            return next;
        });
    }

    return (
        <aside className="flex h-full w-72 shrink-0 flex-col overflow-hidden border-r border-border bg-card lg:w-full">
            {/* On desktop this link lives in the header; there is no room for it there on mobile,
                where the header must fit a burger, and course navigation belongs in here anyway. */}
            <Link
                to={APP_ROUTES.student.myLearning}
                className="flex items-center gap-2 border-b border-border px-4 py-3 text-sm text-muted-foreground transition-colors hover:bg-secondary hover:text-foreground lg:hidden"
            >
                <ArrowLeft className="size-4 shrink-0" />
                {t('header.myLearning')}
            </Link>

            <div className="flex items-center justify-between border-b border-border p-4">
                <span className="text-sm font-semibold uppercase tracking-wider text-foreground">
                    {t('sidebar.courseContent')}
                </span>
                {onClose && (
                    <button
                        type="button"
                        onClick={onClose}
                        title={t('sidebar.collapse')}
                        className="rounded-md p-1 text-muted-foreground hover:bg-secondary hover:text-foreground"
                    >
                        <span className="sr-only">{t('sidebar.collapse')}</span>
                        <X className="size-4" />
                    </button>
                )}
            </div>

            <nav className="flex-1 overflow-y-auto">
                {sections.map((section, sIdx) => {
                    const isOpen = openSections.has(section.sectionId);
                    const sectionCompleted = section.lessons.filter((l) => l.isCompleted).length;
                    const sectionTotal = section.lessons.length;

                    // Lessons are numbered across the whole course, not restarted per section, so the
                    // number matches how a student refers to "lesson 5".
                    const lessonNumberOffset = sections
                        .slice(0, sIdx)
                        .reduce((sum, s) => sum + s.lessons.length, 0);

                    return (
                        <div key={section.sectionId}>
                            {/* Rules, not fills, separate a section from its lessons: a line above
                                always, and one below only while it is collapsed — an open section is
                                continuous with the lessons it just revealed. A tinted strip would
                                fight the active lesson for attention, which in dark mode is the one
                                thing that may be bright. */}
                            <button
                                type="button"
                                onClick={() => toggleSection(section.sectionId)}
                                className={cn(
                                    'flex w-full items-center justify-between gap-2 border-border px-4 py-3 text-left transition-colors hover:bg-secondary/40',
                                    // The panel header already draws the rule above the first section.
                                    sIdx > 0 && 'border-t',
                                    !isOpen && 'border-b',
                                )}
                            >
                                <span className="flex-1 text-sm font-semibold leading-snug text-foreground">
                                    {t('sidebar.sectionPrefix')} {sIdx + 1} · {section.title}
                                </span>
                                <div className="flex items-center gap-2">
                                    <span className="text-xs font-medium text-muted-foreground">
                                        {sectionCompleted} / {sectionTotal}
                                    </span>
                                    <ChevronDown
                                        className={cn(
                                            'h-3.5 w-3.5 shrink-0 text-muted-foreground transition-transform duration-200',
                                            isOpen && 'rotate-180',
                                        )}
                                    />
                                </div>
                            </button>

                            {isOpen && (
                                <ul>
                                    {section.lessons
                                        .slice()
                                        .sort((a, b) => a.displayOrder - b.displayOrder)
                                        .map((lesson, lIdx) => {
                                            const isActive = lesson.lessonId === currentLessonId;
                                            const lessonNumber = lessonNumberOffset + lIdx + 1;
                                            const Icon =
                                                lessonTypeIcon[
                                                    lesson.lessonType as keyof typeof lessonTypeIcon
                                                ] ?? FileText;

                                            return (
                                                <li key={lesson.lessonId}>
                                                    <Link
                                                        to={APP_ROUTES.student.learnLesson(
                                                            courseId,
                                                            lesson.lessonId,
                                                        )}
                                                        className={cn(
                                                            'flex items-start gap-2 px-4 py-2.5 text-sm transition-colors hover:bg-secondary',
                                                            isActive
                                                                ? 'border-l-2 border-primary bg-secondary font-medium text-foreground'
                                                                : 'text-muted-foreground',
                                                        )}
                                                    >
                                                        <span className="w-5 shrink-0 text-right text-xs tabular-nums leading-5 text-muted-foreground/70">
                                                            {t('sidebar.lessonNumber', {
                                                                n: lessonNumber,
                                                            })}
                                                        </span>

                                                        <span className="min-w-0 flex-1">
                                                            <span className="line-clamp-2 block leading-snug">
                                                                {lesson.title}
                                                            </span>
                                                            <span className="mt-0.5 flex items-center gap-1.5 text-xs text-muted-foreground/70">
                                                                <Icon className="size-3.5 shrink-0" />
                                                                {/* A quiz has no length of its own;
                                                                    its question count is the closest
                                                                    thing a student can plan around. */}
                                                                <span>{formatMeta(lesson)}</span>
                                                            </span>
                                                        </span>

                                                        {lesson.isCompleted && (
                                                            <CheckCircle2 className="mt-0.5 size-4 shrink-0 text-success" />
                                                        )}
                                                    </Link>
                                                </li>
                                            );
                                        })}
                                </ul>
                            )}
                        </div>
                    );
                })}
            </nav>

            {/* Mobile-only footer: theme & language controls */}
            <div className="shrink-0 border-t border-border lg:hidden">
                <ThemeSwitcher variant="mobileMenu" />
                <LanguageSwitcher variant="mobileMenu" />
            </div>
        </aside>
    );
}
