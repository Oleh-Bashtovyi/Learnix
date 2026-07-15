import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { BookOpen, Loader2, X } from 'lucide-react';
import { enrollmentsApi } from '@/api/enrollments.api';
import { queryKeys } from '@/api/queryKeys';

interface NewMessageModalProps {
    onClose: () => void;
    onSelectCourse: (courseId: string) => void;
    isStarting: boolean;
}

export function NewMessageModal({ onClose, onSelectCourse, isStarting }: NewMessageModalProps) {
    const { t } = useTranslation('messages');

    const { data: enrollmentsData, isLoading } = useQuery({
        queryKey: queryKeys.enrollments.mine(),
        queryFn: () => enrollmentsApi.getMyEnrollments(0, 100),
    });

    const enrollments = enrollmentsData?.items ?? [];

    return (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4">
            <div className="flex max-h-[80vh] w-full max-w-md flex-col overflow-hidden rounded-xl border border-border bg-card shadow-lg">
                <div className="flex shrink-0 items-center justify-between border-b border-border px-5 py-4">
                    <h2 className="font-heading font-semibold text-foreground">
                        {t('newMessageTitle')}
                    </h2>
                    <button
                        onClick={onClose}
                        disabled={isStarting}
                        className="rounded p-1 text-muted-foreground transition-colors hover:bg-secondary hover:text-foreground disabled:opacity-50"
                    >
                        <X size={16} />
                    </button>
                </div>

                <div className="flex-1 overflow-y-auto p-2">
                    <div className="px-3 pb-2 pt-1">
                        <p className="text-sm text-muted-foreground">
                            {t('selectCourseToMessage')}
                        </p>
                    </div>

                    {isLoading ? (
                        <div className="flex justify-center py-10">
                            <Loader2 className="size-6 animate-spin text-muted-foreground" />
                        </div>
                    ) : enrollments.length === 0 ? (
                        <div className="px-3 py-6 text-center text-sm text-muted-foreground">
                            {t('noCourses')}
                        </div>
                    ) : (
                        <div className="space-y-1">
                            {enrollments.map((enr) => (
                                <button
                                    key={enr.courseId}
                                    onClick={() => onSelectCourse(enr.courseId)}
                                    disabled={isStarting}
                                    className="flex w-full items-center gap-3 rounded-lg p-3 text-left transition-colors hover:bg-secondary disabled:opacity-50"
                                >
                                    <div className="flex size-10 shrink-0 items-center justify-center overflow-hidden rounded bg-muted">
                                        {enr.coverImageUrl ? (
                                            <img
                                                src={enr.coverImageUrl}
                                                alt={enr.courseTitle}
                                                className="size-full object-cover"
                                            />
                                        ) : (
                                            <BookOpen className="size-5 text-muted-foreground" />
                                        )}
                                    </div>
                                    <div className="min-w-0 flex-1">
                                        <p className="truncate text-sm font-medium text-foreground">
                                            {enr.courseTitle}
                                        </p>
                                    </div>
                                </button>
                            ))}
                        </div>
                    )}
                </div>
            </div>
        </div>
    );
}
