import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { ArchiveRestore, CheckCircle, XCircle } from 'lucide-react';
import { ConfirmDialog } from '@/components/common/ui/ConfirmDialog';
import { useCourseForEdit } from '@/hooks/instructor/useCourseForEdit';
import {
    useCreateCourse,
    usePublishCourse,
    useUnarchiveCourse,
    useUnpublishCourse,
    useUpdateCourse,
} from '@/hooks/instructor/useCourseMutations';
import { APP_ROUTES } from '@/routes/paths';
import type { CourseInfoFormData } from '@/schemas/course.schema';
import type { CourseForEditDto } from '@/types/course.types';
import { cn } from '@/utils/cn';
import { CourseInfoForm } from './components/CourseInfoForm';
import { CurriculumTab } from './components/CurriculumTab';

type Tab = 'info' | 'curriculum';

export default function CourseEditorPage() {
    const { id } = useParams<{ id: string }>();
    const navigate = useNavigate();
    const { t } = useTranslation('instructor');
    const [tab, setTab] = useState<Tab>('info');
    const [showPublishConfirm, setShowPublishConfirm] = useState(false);
    const [showUnpublishConfirm, setShowUnpublishConfirm] = useState(false);

    const { data: course, isLoading } = useCourseForEdit(id);
    const createCourse = useCreateCourse();
    const updateCourse = useUpdateCourse(id ?? '');
    const publishCourse = usePublishCourse();
    const unpublishCourse = useUnpublishCourse();

    const unarchiveCourse = useUnarchiveCourse();

    const isNew = !id;
    const isArchived = course?.status === 'Archived';
    const title = isNew ? t('editorTitleNew') : (course?.title ?? '...');

    async function handleInfoSubmit(data: CourseInfoFormData) {
        if (isNew) {
            createCourse.mutate(
                {
                    categoryId: data.categoryId,
                    title: data.title,
                    description: data.description,
                    price: data.price,
                    tags: data.tags,
                },
                {
                    onSuccess: (res) => {
                        navigate(`/instructor/courses/${res.courseId}/edit`, { replace: true });
                    },
                },
            );
        } else {
            updateCourse.mutate({
                categoryId: data.categoryId,
                title: data.title,
                description: data.description,
                price: data.price,
                coverImageUrl: data.coverImageUrl ?? null,
                tags: data.tags,
            });
        }
    }

    const isSaving = createCourse.isPending || updateCourse.isPending;
    const isPublished = course?.status === 'Published';

    function renderPublishChecklist(c: CourseForEditDto) {
        const hasCover = !!c.coverImageUrl;
        const hasSections = c.sections.length > 0;
        const allSectionsHaveLessons =
            c.sections.length > 0 && c.sections.every((s) => s.lessons.length > 0);

        const items = [
            { label: t('publishChecklistCover'), ok: hasCover },
            { label: t('publishChecklistSections'), ok: hasSections },
            { label: t('publishChecklistLessons'), ok: allSectionsHaveLessons },
        ];

        return (
            <div className="rounded-xl border border-warning/30 bg-warning/10 p-4">
                <p className="mb-2 font-medium text-warning">{t('publishChecklistTitle')}</p>
                <ul className="space-y-1 text-sm text-muted-foreground">
                    {items.map(({ label, ok }) => (
                        <li key={label} className="flex items-center gap-2">
                            {ok ? (
                                <CheckCircle size={14} className="text-success" />
                            ) : (
                                <XCircle size={14} className="text-destructive" />
                            )}
                            {label}
                        </li>
                    ))}
                </ul>
            </div>
        );
    }

    return (
        <div className="flex min-h-screen flex-col">
            {/* Header */}
            <header className="border-b border-border bg-card">
                <div className="flex h-14 items-center justify-between px-6">
                    <div className="flex min-w-0 items-center gap-3">
                        <Link
                            to={APP_ROUTES.instructor.dashboard}
                            className="shrink-0 text-sm text-muted-foreground transition-colors hover:text-foreground"
                        >
                            {t('editorBack')}
                        </Link>
                        <span className="text-muted-foreground">·</span>
                        <span className="truncate font-medium text-foreground">{title}</span>
                        {isSaving && (
                            <span className="shrink-0 rounded bg-warning/20 px-2 py-0.5 text-xs text-warning">
                                {t('editorUnsaved')}
                            </span>
                        )}
                    </div>
                    <div className="flex items-center gap-2">
                        {!isNew && isArchived && (
                            <button
                                onClick={() => unarchiveCourse.mutate(id!)}
                                disabled={unarchiveCourse.isPending}
                                className="flex items-center gap-1.5 rounded-lg border border-border px-4 py-1.5 text-sm hover:bg-secondary disabled:opacity-60"
                            >
                                <ArchiveRestore size={14} />
                                {t('btnUnarchiveCourse')}
                            </button>
                        )}
                        {!isNew && course && !isArchived && (
                            <>
                                {isPublished ? (
                                    <button
                                        onClick={() => setShowUnpublishConfirm(true)}
                                        disabled={unpublishCourse.isPending}
                                        className="text-warning-foreground rounded-lg bg-warning px-4 py-1.5 text-sm font-medium transition-colors hover:bg-warning/90 disabled:opacity-60"
                                    >
                                        {t('btnUnpublishCourse')}
                                    </button>
                                ) : (
                                    <button
                                        onClick={() => setShowPublishConfirm(true)}
                                        disabled={publishCourse.isPending}
                                        className="text-success-foreground rounded-lg bg-success px-4 py-1.5 text-sm font-medium transition-colors hover:bg-success/90 disabled:opacity-60"
                                    >
                                        {t('btnPublishCourse')}
                                    </button>
                                )}
                            </>
                        )}
                    </div>
                </div>

                {/* Tabs */}
                <div className="flex gap-6 border-t border-border px-6 text-sm">
                    {(['info', 'curriculum'] as Tab[]).map((tabKey) => (
                        <button
                            key={tabKey}
                            onClick={() => setTab(tabKey)}
                            className={cn(
                                'border-b-2 py-3 transition-colors',
                                tab === tabKey
                                    ? 'border-primary font-medium text-primary'
                                    : 'border-transparent text-muted-foreground hover:text-foreground',
                            )}
                        >
                            {tabKey === 'info' ? t('tabInfo') : t('tabCurriculum')}
                        </button>
                    ))}
                </div>
            </header>

            {/* Archived banner */}
            {!isNew && isArchived && (
                <div className="border-b border-warning/30 bg-warning/10 px-6 py-3 text-sm text-warning">
                    {t('editorArchivedBanner')}
                </div>
            )}

            {/* Content */}
            {isLoading && !isNew ? (
                <div className="flex flex-1 items-center justify-center text-sm text-muted-foreground">
                    Loading course...
                </div>
            ) : (
                <div className="mx-auto w-full max-w-6xl px-6 py-8">
                    {tab === 'info' && (
                        <div className="grid gap-8 md:grid-cols-[1fr_320px]">
                            <div className="rounded-xl border border-border bg-card p-6">
                                <h3 className="mb-5 font-heading text-lg font-semibold">
                                    {t('tabInfo')}
                                </h3>
                                <CourseInfoForm
                                    course={course}
                                    isPending={isSaving}
                                    onSubmit={handleInfoSubmit}
                                />
                            </div>
                            <aside className="space-y-4">
                                {!isNew && course && renderPublishChecklist(course)}
                            </aside>
                        </div>
                    )}

                    {tab === 'curriculum' && !isNew && id && course && (
                        <div className="rounded-xl border border-border bg-card p-6">
                            <CurriculumTab courseId={id} sections={course.sections} />
                        </div>
                    )}

                    {tab === 'curriculum' && isNew && (
                        <div className="rounded-xl border border-dashed border-border py-16 text-center text-sm text-muted-foreground">
                            Save the course info first to start building the curriculum.
                        </div>
                    )}
                </div>
            )}

            {showPublishConfirm && (
                <ConfirmDialog
                    title={t('confirmPublishTitle')}
                    description={t('confirmPublishDesc')}
                    confirmLabel={t('btnPublishCourse')}
                    onConfirm={() => {
                        publishCourse.mutate(id!);
                        setShowPublishConfirm(false);
                    }}
                    onClose={() => setShowPublishConfirm(false)}
                />
            )}

            {showUnpublishConfirm && (
                <ConfirmDialog
                    title={t('confirmUnpublishTitle')}
                    description={t('confirmUnpublishDesc')}
                    confirmLabel={t('btnUnpublishCourse')}
                    variant="destructive"
                    onConfirm={() => {
                        unpublishCourse.mutate(id!);
                        setShowUnpublishConfirm(false);
                    }}
                    onClose={() => setShowUnpublishConfirm(false)}
                />
            )}
        </div>
    );
}
