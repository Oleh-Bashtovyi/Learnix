import { useTranslation } from 'react-i18next';
import { useSortable } from '@dnd-kit/sortable';
import { CSS } from '@dnd-kit/utilities';
import { Eye, EyeOff, GripVertical, Pencil, Trash2 } from 'lucide-react';
import { LessonType } from '@/enums/lesson.enums';
import { useFormatDuration } from '@/hooks/shared/useFormatDuration';
import type { CourseForEditLessonDto } from '@/types/course.types';
import { cn } from '@/utils/cn';

// Video draws on --brand rather than --primary: --primary is near-white in the dark theme, while
// --brand holds the same blue in both, next to the teal Post and amber Test badges.
const TYPE_STYLES: Record<LessonType, string> = {
    Video: 'bg-brand/10 text-brand',
    Post: 'bg-accent/10 text-accent-strong',
    Test: 'bg-warning/20 text-warning',
};

interface Props {
    lesson: CourseForEditLessonDto;
    onEdit: () => void;
    onDelete: () => void;
    onToggleVisibility: () => void;
}

export function LessonRow({ lesson, onEdit, onDelete, onToggleVisibility }: Props) {
    const { t } = useTranslation('instructor');
    const formatDuration = useFormatDuration();
    const { attributes, listeners, setNodeRef, transform, transition, isDragging } = useSortable({
        id: lesson.id,
    });

    // Every lesson type carries a length: a video its duration, a post its estimated reading time
    // (the server derives it from the content, the same figure students see in the curriculum), a
    // test the number of questions it asks.
    function lessonMeta(): string {
        if (lesson.lessonType === 'Video' && lesson.durationSeconds) {
            return formatDuration(lesson.durationSeconds);
        }
        if (lesson.lessonType === 'Post' && lesson.readingSeconds) {
            return formatDuration(lesson.readingSeconds);
        }
        if (lesson.lessonType === 'Test' && lesson.questions.length > 0) {
            return t('common:lessonMeta.questionsCount', { count: lesson.questions.length });
        }
        return '';
    }

    const style = {
        transform: CSS.Transform.toString(transform),
        transition,
        opacity: isDragging ? 0.4 : 1,
    };

    const TYPE_LABELS: Record<LessonType, string> = {
        Video: t('common:general.video'),
        Post: t('badgePost'),
        Test: t('common:general.test'),
    };

    return (
        <div
            ref={setNodeRef}
            style={style}
            className="flex items-center gap-3 border-b border-border px-3 py-2.5 last:border-0"
        >
            <button
                {...attributes}
                {...listeners}
                className="cursor-grab text-muted-foreground active:cursor-grabbing"
            >
                <GripVertical size={14} />
            </button>
            <span
                className={cn(
                    'shrink-0 rounded px-2 py-0.5 text-xs font-medium',
                    TYPE_STYLES[lesson.lessonType],
                )}
            >
                {TYPE_LABELS[lesson.lessonType]}
            </span>
            <span className="flex-1 truncate text-sm text-foreground">{lesson.title}</span>
            <span className="shrink-0 text-xs tabular-nums text-muted-foreground">
                {lessonMeta()}
            </span>
            {/* The actions are their own group, spaced away from the meta text so the duration does
                not read as a fourth control in the button strip. */}
            <div className="flex shrink-0 items-center gap-1 pl-3">
                <button
                    onClick={onToggleVisibility}
                    className="rounded p-1 text-muted-foreground transition-colors hover:bg-hover hover:text-primary"
                >
                    {lesson.isHidden ? <EyeOff size={14} /> : <Eye size={14} />}
                </button>
                <button
                    onClick={onEdit}
                    className="rounded p-1 text-muted-foreground transition-colors hover:bg-hover hover:text-primary"
                >
                    <Pencil size={14} />
                </button>
                <button
                    onClick={onDelete}
                    className="rounded p-1 text-muted-foreground transition-colors hover:bg-hover hover:text-destructive"
                >
                    <Trash2 size={14} />
                </button>
            </div>
        </div>
    );
}
