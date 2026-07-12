import { useTranslation } from 'react-i18next';
import { Eye, EyeOff, RefreshCw, Trash2 } from 'lucide-react';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { TableCell, TableRow } from '@/components/ui/table';
import { CourseStatus } from '@/enums/course.enums';
import type { ManageCourseCardDto } from '@/types/course.types';
import { cn } from '@/utils/cn';
import type { PendingAction } from '../CourseModerationPage';

const STATUS_STYLES: Record<CourseStatus, string> = {
    Published: 'bg-success/20 text-success',
    Draft: 'bg-muted text-muted-foreground',
    Archived: 'bg-warning/20 text-warning',
};

const STATUS_VARIANTS: Record<CourseStatus, 'outline' | 'secondary' | 'default'> = {
    Published: 'outline',
    Draft: 'default',
    Archived: 'secondary',
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
        Published: t('common:status.published'),
        Draft: t('common:status.draft'),
        Archived: t('common:status.archived'),
    };

    return (
        <TableRow className={cn('hover:bg-secondary/30', c.isDeleted && 'opacity-50')}>
            {/* Course */}
            <TableCell className="px-5 py-3">
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
            </TableCell>

            {/* Status */}
            <TableCell className="px-5 py-3">
                <Badge
                    variant={STATUS_VARIANTS[c.status] ?? 'default'}
                    className={cn(
                        'border-transparent',
                        STATUS_STYLES[c.status] ?? 'bg-muted text-muted-foreground hover:bg-muted',
                    )}
                >
                    {STATUS_LABELS[c.status] ?? c.status}
                </Badge>
            </TableCell>

            {/* Enrollments */}
            <TableCell className="px-5 py-3 text-muted-foreground">{c.enrollmentsCount}</TableCell>

            {/* Price */}
            <TableCell className="px-5 py-3 text-muted-foreground">
                {c.isFree ? t('common:general.free') : `$${c.price.toFixed(2)}`}
            </TableCell>

            {/* Actions */}
            <TableCell className="px-5 py-3">
                <div className="flex items-center justify-end gap-1">
                    {!c.isDeleted && c.status === 'Draft' && (
                        <Button
                            variant="ghost"
                            size="icon"
                            onClick={() => onSetPending({ type: 'publish', course: c })}
                            className="size-8 text-muted-foreground hover:bg-success/10 hover:text-success"
                            title={t('common:actions.publish')}
                        >
                            <Eye size={14} />
                        </Button>
                    )}
                    {!c.isDeleted && c.status === 'Published' && (
                        <Button
                            variant="ghost"
                            size="icon"
                            onClick={() => onSetPending({ type: 'unpublish', course: c })}
                            className="size-8 text-muted-foreground hover:bg-warning/10 hover:text-warning"
                            title={t('common:actions.unpublish')}
                        >
                            <EyeOff size={14} />
                        </Button>
                    )}
                    {c.isDeleted ? (
                        <Button
                            variant="ghost"
                            size="icon"
                            onClick={() => onSetPending({ type: 'recover', course: c })}
                            className="size-8 text-muted-foreground hover:bg-success/10 hover:text-success"
                            title={t('btnRecoverCourse')}
                        >
                            <RefreshCw size={14} />
                        </Button>
                    ) : (
                        <Button
                            variant="ghost"
                            size="icon"
                            onClick={() => onSetPending({ type: 'delete', course: c })}
                            className="size-8 text-muted-foreground hover:bg-destructive/10 hover:text-destructive"
                            title={t('common:actions.delete')}
                        >
                            <Trash2 size={14} />
                        </Button>
                    )}
                </div>
            </TableCell>
        </TableRow>
    );
}
