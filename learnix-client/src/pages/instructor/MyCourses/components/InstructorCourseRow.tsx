import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';
import type { UseMutationResult } from '@tanstack/react-query';
import { Archive, ArchiveRestore, EyeOff, Globe, Pencil, Trash2 } from 'lucide-react';
import { APP_ROUTES } from '@/config/routes';
import { CourseStatus } from '@/enums/course.enums';
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
        <tr className="hover:bg-secondary/30">
            <td className="px-5 py-3">
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
            </td>
            <td className="px-5 py-3">
                <span
                    className={cn(
                        'rounded px-2 py-0.5 text-xs font-medium',
                        STATUS_STYLES[course.status],
                    )}
                >
                    {STATUS_LABELS[course.status]}
                </span>
            </td>
            <td className="px-5 py-3 text-muted-foreground">{course.enrollmentsCount}</td>
            <td className="px-5 py-3">
                <div className="flex items-center justify-end gap-1">
                    <button
                        onClick={() => navigate(APP_ROUTES.instructor.editCourse(course.id))}
                        className="rounded p-1.5 text-muted-foreground transition-colors hover:bg-secondary hover:text-primary"
                        title={t('btnEdit')}
                    >
                        <Pencil size={14} />
                    </button>
                    {course.status === 'Draft' && (
                        <button
                            onClick={() => publishMutation.mutate(course.id)}
                            className="rounded p-1.5 text-muted-foreground transition-colors hover:bg-secondary hover:text-success"
                            title={t('btnPublish')}
                        >
                            <Globe size={14} />
                        </button>
                    )}
                    {course.status === 'Published' && (
                        <button
                            onClick={() => unpublishMutation.mutate(course.id)}
                            className="rounded p-1.5 text-muted-foreground transition-colors hover:bg-secondary hover:text-warning"
                            title={t('btnUnpublish')}
                        >
                            <EyeOff size={14} />
                        </button>
                    )}
                    {course.status !== 'Archived' && (
                        <button
                            onClick={() => onArchive(course)}
                            className="rounded p-1.5 text-muted-foreground transition-colors hover:bg-secondary hover:text-warning"
                            title={t('btnArchive')}
                        >
                            <Archive size={14} />
                        </button>
                    )}
                    {course.status === 'Archived' && (
                        <button
                            onClick={() => unarchiveMutation.mutate(course.id)}
                            className="rounded p-1.5 text-muted-foreground transition-colors hover:bg-secondary hover:text-success"
                            title={t('btnUnarchive')}
                        >
                            <ArchiveRestore size={14} />
                        </button>
                    )}
                    <button
                        onClick={() => onDelete(course)}
                        className="rounded p-1.5 text-muted-foreground transition-colors hover:bg-secondary hover:text-destructive"
                        title={t('btnDelete')}
                    >
                        <Trash2 size={14} />
                    </button>
                </div>
            </td>
        </tr>
    );
}
