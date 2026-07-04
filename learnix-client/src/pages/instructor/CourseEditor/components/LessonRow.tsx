import { useTranslation } from 'react-i18next';
import { useSortable } from '@dnd-kit/sortable';
import { CSS } from '@dnd-kit/utilities';
import { Eye, EyeOff, GripVertical, Pencil, Trash2 } from 'lucide-react';
import { LessonType } from '@/enums/lesson.enums';
import type { CourseForEditLessonDto } from '@/types/course.types';
import { cn } from '@/utils/cn';

const TYPE_STYLES: Record<LessonType, string> = {
    Video: 'bg-primary/10 text-primary',
    Post: 'bg-accent/10 text-accent',
    Test: 'bg-warning/20 text-warning',
};

function lessonMeta(lesson: CourseForEditLessonDto): string {
    if (lesson.lessonType === 'Video' && lesson.durationSeconds) {
        const m = Math.floor(lesson.durationSeconds / 60);
        const s = lesson.durationSeconds % 60;
        return `${m}:${String(s).padStart(2, '0')}`;
    }
    if (lesson.lessonType === 'Test' && lesson.questions.length > 0) {
        return `${lesson.questions.length} questions`;
    }
    return '';
}

interface Props {
    lesson: CourseForEditLessonDto;
    onEdit: () => void;
    onDelete: () => void;
    onToggleVisibility: () => void;
}

export function LessonRow({ lesson, onEdit, onDelete, onToggleVisibility }: Props) {
    const { t } = useTranslation('instructor');
    const { attributes, listeners, setNodeRef, transform, transition, isDragging } = useSortable({
        id: lesson.id,
    });

    const style = {
        transform: CSS.Transform.toString(transform),
        transition,
        opacity: isDragging ? 0.4 : 1,
    };

    const TYPE_LABELS: Record<LessonType, string> = {
        Video: t('badgeVideo'),
        Post: t('badgePost'),
        Test: t('badgeTest'),
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
            <span className="shrink-0 text-xs text-muted-foreground">{lessonMeta(lesson)}</span>
            <button
                onClick={onToggleVisibility}
                className="text-muted-foreground transition-colors hover:text-primary"
            >
                {lesson.isHidden ? <EyeOff size={14} /> : <Eye size={14} />}
            </button>
            <button
                onClick={onEdit}
                className="text-muted-foreground transition-colors hover:text-primary"
            >
                <Pencil size={14} />
            </button>
            <button
                onClick={onDelete}
                className="text-muted-foreground transition-colors hover:text-destructive"
            >
                <Trash2 size={14} />
            </button>
        </div>
    );
}
