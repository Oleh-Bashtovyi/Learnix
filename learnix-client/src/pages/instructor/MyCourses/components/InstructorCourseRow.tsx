import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';
import type { UseMutationResult } from '@tanstack/react-query';
import {
    Archive,
    ArchiveRestore,
    ClipboardList,
    DollarSign,
    ExternalLink,
    EyeOff,
    Globe,
    MoreVertical,
    Pencil,
    Star,
    Trash2,
} from 'lucide-react';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import {
    DropdownMenu,
    DropdownMenuContent,
    DropdownMenuItem,
    DropdownMenuSeparator,
    DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
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
        Published: t('common:status.published'),
        Draft: t('common:status.draft'),
        Archived: t('common:status.archived'),
    };

    // A draft never had a chance to enroll anyone, so there is nothing yet for these to show —
    // Published and Archived (which may have enrolled students in its past) both qualify.
    const hasHistory = course.status !== 'Draft';

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
                        title={t('common:actions.edit')}
                    >
                        <Pencil size={14} />
                    </Button>
                    {hasHistory && (
                        <>
                            <Button
                                variant="ghost"
                                size="icon"
                                onClick={() =>
                                    navigate(
                                        `${APP_ROUTES.instructor.analytics}?tab=reviews&courseId=${course.id}`,
                                    )
                                }
                                className="size-8 text-muted-foreground hover:bg-warning/10 hover:text-warning"
                                title={t('viewReviews')}
                            >
                                <Star size={14} />
                            </Button>
                            <Button
                                variant="ghost"
                                size="icon"
                                onClick={() =>
                                    navigate(`${APP_ROUTES.instructor.analytics}?tab=earnings`)
                                }
                                className="size-8 text-muted-foreground hover:bg-success/10 hover:text-success"
                                title={t('viewEarnings')}
                            >
                                <DollarSign size={14} />
                            </Button>
                        </>
                    )}

                    <DropdownMenu>
                        <DropdownMenuTrigger asChild>
                            <Button
                                variant="ghost"
                                size="icon"
                                className="size-8 text-muted-foreground hover:bg-secondary hover:text-foreground"
                                title={t('moreActions')}
                            >
                                <MoreVertical size={14} />
                            </Button>
                        </DropdownMenuTrigger>
                        <DropdownMenuContent align="end">
                            {hasHistory && (
                                <DropdownMenuItem
                                    className="cursor-pointer gap-2"
                                    onClick={() =>
                                        navigate(
                                            `${APP_ROUTES.instructor.analytics}?tab=tests&courseId=${course.id}`,
                                        )
                                    }
                                >
                                    <ClipboardList size={14} />
                                    {t('viewTestResults')}
                                </DropdownMenuItem>
                            )}
                            {/* A course is only reachable from the catalog once published. */}
                            {course.status === 'Published' && (
                                <DropdownMenuItem asChild className="cursor-pointer">
                                    <a
                                        href={APP_ROUTES.public.courseDetail(course.id)}
                                        target="_blank"
                                        rel="noreferrer"
                                        className="flex items-center gap-2"
                                    >
                                        <ExternalLink size={14} />
                                        {t('editorViewPublicPage')}
                                    </a>
                                </DropdownMenuItem>
                            )}
                            {course.status === 'Draft' && (
                                <DropdownMenuItem
                                    className="cursor-pointer gap-2"
                                    onClick={() => publishMutation.mutate(course.id)}
                                >
                                    <Globe size={14} />
                                    {t('common:actions.publish')}
                                </DropdownMenuItem>
                            )}
                            {course.status === 'Published' && (
                                <DropdownMenuItem
                                    className="cursor-pointer gap-2"
                                    onClick={() => unpublishMutation.mutate(course.id)}
                                >
                                    <EyeOff size={14} />
                                    {t('common:actions.unpublish')}
                                </DropdownMenuItem>
                            )}
                            {course.status !== 'Archived' && (
                                <DropdownMenuItem
                                    className="cursor-pointer gap-2"
                                    onClick={() => onArchive(course)}
                                >
                                    <Archive size={14} />
                                    {t('btnArchive')}
                                </DropdownMenuItem>
                            )}
                            {course.status === 'Archived' && (
                                <DropdownMenuItem
                                    className="cursor-pointer gap-2"
                                    onClick={() => unarchiveMutation.mutate(course.id)}
                                >
                                    <ArchiveRestore size={14} />
                                    {t('btnUnarchive')}
                                </DropdownMenuItem>
                            )}
                            <DropdownMenuSeparator />
                            <DropdownMenuItem
                                className="cursor-pointer gap-2 text-destructive focus:text-destructive"
                                onClick={() => onDelete(course)}
                            >
                                <Trash2 size={14} />
                                {t('common:actions.delete')}
                            </DropdownMenuItem>
                        </DropdownMenuContent>
                    </DropdownMenu>
                </div>
            </TableCell>
        </TableRow>
    );
}
