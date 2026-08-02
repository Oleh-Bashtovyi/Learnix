import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { ChevronDown, ClipboardList, FileText, PlayCircle } from 'lucide-react';
import { TextButton } from '@/components/common/elements/TextButton';
import { useFormatDuration } from '@/hooks/shared/useFormatDuration';
import type { LessonSummaryDto, SectionDetailDto } from '@/types/course.types';
import { cn } from '@/utils/cn';

interface CurriculumAccordionProps {
    sections: SectionDetailDto[];
}

const MAX_VISIBLE_SECTIONS = 10;

// Video draws on --brand rather than --primary: --primary is near-white in the dark theme, while
// --brand holds the same blue in both — kept in sync with the instructor lesson editor's LessonRow.
const LESSON_TYPE_ICONS: Record<LessonSummaryDto['lessonType'], React.ReactNode> = {
    Video: <PlayCircle className="size-4 text-brand" />,
    Post: <FileText className="size-4 text-accent-strong" />,
    Test: <ClipboardList className="size-4 text-warning" />,
};

export function CurriculumAccordion({ sections }: CurriculumAccordionProps) {
    const { t } = useTranslation('courseDetail');
    const [openIds, setOpenIds] = useState<Set<string>>(new Set([sections[0]?.id]));
    const [isExpanded, setIsExpanded] = useState(false);

    // The icon carries the lesson type visually; this names it for assistive tech.
    const lessonTypeLabels: Record<LessonSummaryDto['lessonType'], string> = {
        Video: t('common:general.video'),
        Post: t('common:general.article'),
        Test: t('curriculum.lessonTypes.Test'),
    };

    const formatDuration = useFormatDuration();

    function toggle(id: string) {
        setOpenIds((prev) => {
            const next = new Set(prev);
            if (next.has(id)) {
                next.delete(id);
            } else {
                next.add(id);
            }
            return next;
        });
    }

    const allSectionIds = new Set(sections.map((s) => s.id));
    const allExpanded = openIds.size === sections.length;

    function toggleAll() {
        if (allExpanded) {
            setOpenIds(new Set());
        } else {
            setOpenIds(allSectionIds);
        }
    }

    const totalLessons = sections.reduce((sum, s) => sum + s.lessons.length, 0);

    return (
        <section>
            <h2 className="font-heading text-xl font-semibold text-foreground">
                {t('curriculum.title')}
            </h2>
            <div className="mt-1 flex items-baseline justify-between">
                <span className="whitespace-nowrap text-sm text-muted-foreground">
                    {t('curriculum.sectionCount', { count: sections.length })}
                    {' · '}
                    {t('curriculum.lessonCount', { count: totalLessons })}
                </span>
                <TextButton onClick={toggleAll} className="text-sm">
                    {allExpanded ? t('curriculum.collapseAll') : t('curriculum.expandAll')}
                </TextButton>
            </div>

            <div className="mt-4 divide-y divide-border overflow-hidden rounded-xl border border-border">
                {sections
                    .slice(0, isExpanded ? sections.length : MAX_VISIBLE_SECTIONS)
                    .map((section) => {
                        const isOpen = openIds.has(section.id);
                        return (
                            <div key={section.id}>
                                <button
                                    type="button"
                                    onClick={() => toggle(section.id)}
                                    className="flex w-full items-center justify-between gap-4 bg-muted/30 px-5 py-4 text-left hover:bg-muted/50"
                                >
                                    <span className="font-medium text-foreground">
                                        {section.title}
                                    </span>
                                    <div className="flex shrink-0 items-center gap-3">
                                        <span className="whitespace-nowrap text-sm text-muted-foreground">
                                            {t('curriculum.lessonCount', {
                                                count: section.lessons.length,
                                            })}
                                        </span>
                                        <ChevronDown
                                            className={cn(
                                                'h-4 w-4 text-muted-foreground transition-transform',
                                                isOpen && 'rotate-180',
                                            )}
                                        />
                                    </div>
                                </button>

                                {isOpen && (
                                    <ul className="divide-y divide-border/50">
                                        {section.lessons.map((lesson) => (
                                            <li
                                                key={lesson.id}
                                                className="flex items-center gap-3 px-5 py-3"
                                            >
                                                <span className="shrink-0" aria-hidden="true">
                                                    {LESSON_TYPE_ICONS[lesson.lessonType]}
                                                </span>
                                                <span className="flex-1 text-sm text-foreground">
                                                    {lesson.title}
                                                </span>
                                                <span className="sr-only">
                                                    {lessonTypeLabels[lesson.lessonType]}
                                                </span>
                                                {lesson.lessonType === 'Video' &&
                                                lesson.durationSeconds != null ? (
                                                    <span className="whitespace-nowrap text-xs text-muted-foreground">
                                                        {formatDuration(lesson.durationSeconds)}
                                                    </span>
                                                ) : null}
                                                {lesson.lessonType === 'Post' &&
                                                lesson.readingSeconds != null ? (
                                                    <span className="whitespace-nowrap text-xs text-muted-foreground">
                                                        {formatDuration(lesson.readingSeconds)}
                                                    </span>
                                                ) : null}
                                                {lesson.lessonType === 'Test' &&
                                                lesson.questionsCount != null ? (
                                                    <span className="whitespace-nowrap text-xs text-muted-foreground">
                                                        {t('common:lessonMeta.questionsCount', {
                                                            count: lesson.questionsCount,
                                                        })}
                                                    </span>
                                                ) : null}
                                            </li>
                                        ))}
                                    </ul>
                                )}
                            </div>
                        );
                    })}
            </div>

            {sections.length > MAX_VISIBLE_SECTIONS && (
                <button
                    type="button"
                    onClick={() => setIsExpanded(!isExpanded)}
                    className="mt-4 w-full rounded-xl border border-border bg-background py-3 text-sm font-medium text-foreground transition-colors hover:bg-muted"
                >
                    {isExpanded ? t('common:actions.showLess') : t('common:actions.showMore')}
                </button>
            )}
        </section>
    );
}
