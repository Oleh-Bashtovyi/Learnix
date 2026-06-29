import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';
import type { UseMutationResult } from '@tanstack/react-query';
import { Archive, ArchiveRestore, EyeOff, Globe, Pencil, Trash2 } from 'lucide-react';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { TableCell, TableRow } from '@/components/ui/table';
import { CourseStatus } from '@/enums/course.enums';
import { APP_ROUTES } from '@/routes/paths';
import type { ManageCourseCardDto } from '@/types/course.types';
import { cn } from '@/utils/cn';

const STATUS_STYLES: Record<CourseStatus, string> = {
    Published: 'bg-success/20 text-success',
    Draft: 'bg-muted text-muted-foreground',
    Archived: 'bg-warning/20 text-warning',
};

interface InstructorCourseRowProps {
    course: ManageCourseCardDto;
    onArchive: (course: ManageCourseCardDto) => void;
    onDelete: (course: ManageCourseCardDto) => void;
    publishMutation: UseMutationResult<unknown, Error, string, unknown>;
    unpublishMutation: UseMutationResult<unknown, Error, string, unknown>;
    unarchiveMutation: UseMutationResult<unknown, Error, string, unknown>;
}

export function InstructorCourseRow({
    course,
    onArchive,
    onDelete,
    publishMutation,
    unpublishMutation,
    unarchiveMutation,
}: InstructorCourseRowProps) {
    const { t } = useTranslation('instructor');
    const navigate = useNavigate();

    const STATUS_LABELS: Record<CourseStatus, string> = {
        Published: t('statusPublished'),
        Draft: t('statusDraft'),
        Archived: t('statusArchived'),
    };

    return (
        <TableRow className="hover:bg-secondary/30">
            <TableCell className="px-5 py-3">
                <div className="flex items-center gap-3">
                    <div className="h-10 w-14 shrink-0 overflow-hidden rounded bg-gradient-to-br from-primary/30 to-accent/30">
                        {course.coverImageUrl && (
                            <img
                                src={course.coverImageUrl}
                                alt=""
                                className="size-full object-cover"
                            />
                        )}
                    </div>
                    <span className="font-medium text-foreground">{course.title}</span>
                </div>
            </TableCell>
            <TableCell className="px-5 py-3">
                <Badge
                    variant="secondary"
                    className={cn('border-transparent', STATUS_STYLES[course.status])}
                >
                    {STATUS_LABELS[course.status]}
                </Badge>
            </TableCell>
            <TableCell className="px-5 py-3 text-muted-foreground">
                {course.enrollmentsCount}
            </TableCell>
            <TableCell className="px-5 py-3">
                <div className="flex items-center justify-end gap-1">
                    <Button
                        variant="ghost"
                        size="icon"
                        onClick={() => navigate(APP_ROUTES.instructor.editCourse(course.id))}
                        className="size-8 text-muted-foreground hover:bg-primary/10 hover:text-primary"
                        title={t('btnEdit')}
                    >
                        <Pencil size={14} />
                    </Button>
                    {course.status === 'Draft' && (
                        <Button
                            variant="ghost"
                            size="icon"
                            onClick={() => publishMutation.mutate(course.id)}
                            className="size-8 text-muted-foreground hover:bg-success/10 hover:text-success"
                            title={t('btnPublish')}
                        >
                            <Globe size={14} />
                        </Button>
                    )}
                    {course.status === 'Published' && (
                        <Button
                            variant="ghost"
                            size="icon"
                            onClick={() => unpublishMutation.mutate(course.id)}
                            className="size-8 text-muted-foreground hover:bg-warning/10 hover:text-warning"
                            title={t('btnUnpublish')}
                        >
                            <EyeOff size={14} />
                        </Button>
                    )}
                    {course.status !== 'Archived' && (
                        <Button
                            variant="ghost"
                            size="icon"
                            onClick={() => onArchive(course)}
                            className="size-8 text-muted-foreground hover:bg-warning/10 hover:text-warning"
                            title={t('btnArchive')}
                        >
                            <Archive size={14} />
                        </Button>
                    )}
                    {course.status === 'Archived' && (
                        <Button
                            variant="ghost"
                            size="icon"
                            onClick={() => unarchiveMutation.mutate(course.id)}
                            className="size-8 text-muted-foreground hover:bg-success/10 hover:text-success"
                            title={t('btnUnarchive')}
                        >
                            <ArchiveRestore size={14} />
                        </Button>
                    )}
                    <Button
                        variant="ghost"
                        size="icon"
                        onClick={() => onDelete(course)}
                        className="size-8 text-muted-foreground hover:bg-destructive/10 hover:text-destructive"
                        title={t('btnDelete')}
                    >
                        <Trash2 size={14} />
                    </Button>
                </div>
            </TableCell>
        </TableRow>
    );
}
