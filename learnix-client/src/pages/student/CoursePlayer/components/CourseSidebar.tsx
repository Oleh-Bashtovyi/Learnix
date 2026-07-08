import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import { CheckCircle2, ChevronDown, ClipboardList, FileText, PlayCircle } from 'lucide-react';
import { LanguageSwitcher } from '@/components/common/ui/LanguageSwitcher';
import { ThemeSwitcher } from '@/components/common/ui/ThemeSwitcher';
import { APP_ROUTES } from '@/routes/paths';
import type { SectionProgressDto } from '@/types/progress.types';
import { cn } from '@/utils/cn';

interface CourseSidebarProps {
    sections: SectionProgressDto[];
    currentLessonId: string;
    courseId: string;
    totalLessons: number;
    completedLessons: number;
    onCloseMobile?: () => void;
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
    onCloseMobile,
}: CourseSidebarProps) {
    const { t } = useTranslation('lessonPlayer');

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
            <div className="flex items-center justify-between border-b border-border p-4">
                <span className="text-sm font-semibold uppercase tracking-wider text-foreground">
                    {t('sidebar.courseContent')}
                </span>
                {onCloseMobile && (
                    <button
                        onClick={onCloseMobile}
                        className="rounded-md p-1 text-muted-foreground hover:bg-secondary hover:text-foreground lg:hidden"
                    >
                        <span className="sr-only">Close sidebar</span>
                        <svg
                            xmlns="http://www.w3.org/2000/svg"
                            width="16"
                            height="16"
                            viewBox="0 0 24 24"
                            fill="none"
                            stroke="currentColor"
                            strokeWidth="2"
                            strokeLinecap="round"
                            strokeLinejoin="round"
                        >
                            <path d="M18 6 6 18" />
                            <path d="m6 6 12 12" />
                        </svg>
                    </button>
                )}
            </div>

            <nav className="flex-1 overflow-y-auto py-2">
                {sections.map((section, sIdx) => {
                    const isOpen = openSections.has(section.sectionId);
                    const sectionCompleted = section.lessons.filter((l) => l.isCompleted).length;
                    const sectionTotal = section.lessons.length;

                    return (
                        <div key={section.sectionId} className="mb-1">
                            <button
                                type="button"
                                onClick={() => toggleSection(section.sectionId)}
                                className="flex w-full items-center justify-between px-4 py-2 text-left hover:bg-secondary/50"
                            >
                                <span className="flex-1 text-xs font-semibold uppercase tracking-wider text-muted-foreground">
                                    {t('sidebar.sectionPrefix')} {sIdx + 1} · {section.title}
                                </span>
                                <div className="flex items-center gap-2">
                                    <span className="text-xs font-medium text-muted-foreground/70">
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
                                        .map((lesson) => {
                                            const isActive = lesson.lessonId === currentLessonId;
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
                                                            'flex items-center gap-3 px-4 py-2.5 text-sm transition-colors hover:bg-secondary',
                                                            isActive
                                                                ? 'border-l-2 border-primary bg-secondary font-medium text-foreground'
                                                                : 'text-muted-foreground',
                                                        )}
                                                    >
                                                        <Icon className="size-4 shrink-0 opacity-60" />
                                                        <span className="line-clamp-2 flex-1 leading-snug">
                                                            {lesson.title}
                                                        </span>
                                                        {lesson.isCompleted && (
                                                            <CheckCircle2 className="size-4 shrink-0 text-success" />
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
