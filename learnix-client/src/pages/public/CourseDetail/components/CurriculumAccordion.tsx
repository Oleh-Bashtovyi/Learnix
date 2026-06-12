import { useState } from 'react';
import { ChevronDown, PlayCircle, FileText, ClipboardList } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { cn } from '@/utils/cn';
import type { SectionDetailDto, LessonSummaryDto } from '@/types/course.types';

interface CurriculumAccordionProps {
    sections: SectionDetailDto[];
}

const LESSON_TYPE_ICONS: Record<LessonSummaryDto['lessonType'], React.ReactNode> = {
    Video: <PlayCircle className="h-4 w-4 text-primary" />,
    Post: <FileText className="h-4 w-4 text-accent" />,
    Test: <ClipboardList className="h-4 w-4 text-warning" />,
};

export function CurriculumAccordion({ sections }: CurriculumAccordionProps) {
    const { t } = useTranslation('courseDetail');
    const [openIds, setOpenIds] = useState<Set<string>>(new Set([sections[0]?.id]));

    function toggle(id: string) {
        setOpenIds((prev) => {
            const next = new Set(prev);
            next.has(id) ? next.delete(id) : next.add(id);
            return next;
        });
    }

    const totalLessons = sections.reduce((sum, s) => sum + s.lessons.length, 0);

    return (
        <section>
            <div className="flex items-baseline justify-between">
                <h2 className="font-heading text-xl font-semibold text-foreground">
                    {t('curriculum.title')}
                </h2>
                <span className="text-sm text-muted-foreground">
                    {t('curriculum.sectionCount', { count: sections.length })}
                    {' · '}
                    {t('curriculum.lessonCount', { count: totalLessons })}
                </span>
            </div>

            <div className="mt-4 divide-y divide-border overflow-hidden rounded-xl border border-border">
                {sections.map((section) => {
                    const isOpen = openIds.has(section.id);
                    return (
                        <div key={section.id}>
                            <button
                                type="button"
                                onClick={() => toggle(section.id)}
                                className="flex w-full items-center justify-between bg-muted/30 px-5 py-4 text-left hover:bg-muted/50"
                            >
                                <span className="font-medium text-foreground">{section.title}</span>
                                <div className="flex items-center gap-3">
                                    <span className="text-sm text-muted-foreground">
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
                                            {LESSON_TYPE_ICONS[lesson.lessonType]}
                                            <span className="flex-1 text-sm text-foreground">
                                                {lesson.title}
                                            </span>
                                            <span className="text-xs text-muted-foreground">
                                                {t(`curriculum.lessonTypes.${lesson.lessonType}`)}
                                            </span>
                                        </li>
                                    ))}
                                </ul>
                            )}
                        </div>
                    );
                })}
            </div>
        </section>
    );
}
