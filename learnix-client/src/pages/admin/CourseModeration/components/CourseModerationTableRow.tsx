import { useTranslation } from 'react-i18next';
import { Eye, EyeOff, RefreshCw, Trash2 } from 'lucide-react';
import { CourseStatus } from '@/enums/course.enums';
import type { ManageCourseCardDto } from '@/types/course.types';
import { cn } from '@/utils/cn';
import type { PendingAction } from '../CourseModerationPage';

const STATUS_STYLES: Record<CourseStatus, string> = {
    Published: 'bg-success/20 text-success',
    Draft: 'bg-muted text-muted-foreground',
    Archived: 'bg-warning/20 text-warning',
};

interface CourseModerationTableRowProps {
    course: ManageCourseCardDto;
    onSetPending: (action: PendingAction) => void;
}

export function CourseModerationTableRow({
    course: c,
    onSetPending,
}: CourseModerationTableRowProps) {
    const { t } = useTranslation('admin');

    const STATUS_LABELS: Record<CourseStatus, string> = {
        Published: t('courseStatusPublished'),
        Draft: t('courseStatusDraft'),
        Archived: t('courseStatusArchived'),
    };

    return (
        <tr className={cn('hover:bg-secondary/30', c.isDeleted && 'opacity-50')}>
            {/* Course */}
            <td className="px-5 py-3">
                <div className="flex items-center gap-3">
                    <div className="h-10 w-14 shrink-0 overflow-hidden rounded bg-gradient-to-br from-primary/30 to-accent/30">
                        {c.coverImageUrl && (
                            <img src={c.coverImageUrl} alt="" className="size-full object-cover" />
                        )}
                    </div>
                    <div>
                        <p className="font-medium text-foreground">{c.title}</p>
                        {c.isDeleted && (
                            <span className="text-xs text-destructive">
                                {t('courseStatusDeleted')}
                            </span>
                        )}
                    </div>
                </div>
            </td>

            {/* Status */}
            <td className="px-5 py-3">
                <span
                    className={cn(
                        'rounded px-2 py-0.5 text-xs font-medium',
                        STATUS_STYLES[c.status] ?? 'bg-muted text-muted-foreground',
                    )}
                >
                    {STATUS_LABELS[c.status] ?? c.status}
                </span>
            </td>

            {/* Enrollments */}
            <td className="px-5 py-3 text-muted-foreground">{c.enrollmentsCount}</td>

            {/* Price */}
            <td className="px-5 py-3 text-muted-foreground">
                {c.isFree ? t('courseFree') : `$${c.price.toFixed(2)}`}
            </td>

            {/* Actions */}
            <td className="px-5 py-3">
                <div className="flex items-center justify-end gap-1">
                    {!c.isDeleted && c.status === 'Draft' && (
                        <button
                            onClick={() => onSetPending({ type: 'publish', course: c })}
                            className="rounded p-1.5 text-muted-foreground transition-colors hover:bg-secondary hover:text-success"
                            title={t('btnPublish')}
                        >
                            <Eye size={14} />
                        </button>
                    )}
                    {!c.isDeleted && c.status === 'Published' && (
                        <button
                            onClick={() => onSetPending({ type: 'unpublish', course: c })}
                            className="rounded p-1.5 text-muted-foreground transition-colors hover:bg-secondary hover:text-warning"
                            title={t('btnUnpublish')}
                        >
                            <EyeOff size={14} />
                        </button>
                    )}
                    {c.isDeleted ? (
                        <button
                            onClick={() => onSetPending({ type: 'recover', course: c })}
                            className="rounded p-1.5 text-muted-foreground transition-colors hover:bg-secondary hover:text-success"
                            title={t('btnRecoverCourse')}
                        >
                            <RefreshCw size={14} />
                        </button>
                    ) : (
                        <button
                            onClick={() => onSetPending({ type: 'delete', course: c })}
                            className="rounded p-1.5 text-muted-foreground transition-colors hover:bg-secondary hover:text-destructive"
                            title={t('btnDeleteCourse')}
                        >
                            <Trash2 size={14} />
                        </button>
                    )}
                </div>
            </td>
        </tr>
    );
}
