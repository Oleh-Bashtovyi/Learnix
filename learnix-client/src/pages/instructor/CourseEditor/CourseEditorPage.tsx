import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate, useParams } from 'react-router-dom';
import { ArchiveRestore, CheckCircle, Circle, ExternalLink, Eye } from 'lucide-react';
import { ConfirmDialog } from '@/components/common/elements/ConfirmDialog';
import { AsyncButton } from '@/components/ui/async-button';
import { Button } from '@/components/ui/button';
import type { CourseStatus } from '@/enums/course.enums';
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
import { CoursePreviewModal } from './components/CoursePreviewModal';
import { CurriculumTab } from './components/CurriculumTab';

type Tab = 'info' | 'curriculum';

export default function CourseEditorPage() {
    const { id } = useParams<{ id: string }>();
    const navigate = useNavigate();
    const { t, i18n } = useTranslation('instructor');
    const [tab, setTab] = useState<Tab>('info');
    const [showPublishConfirm, setShowPublishConfirm] = useState(false);
    const [showUnpublishConfirm, setShowUnpublishConfirm] = useState(false);
    const [showPreview, setShowPreview] = useState(false);
    const [isInfoDirty, setIsInfoDirty] = useState(false);

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
                        navigate(APP_ROUTES.instructor.editCourse(res.courseId), { replace: true });
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

    // Shown on both tabs and on a course that has not been saved yet: the cover is met on Course
    // info, the sections and lessons on Curriculum, so no single tab can satisfy the whole list.
    function renderPublishChecklist(c: CourseForEditDto | undefined) {
        const hasCover = !!c?.coverImageUrl;
        const hasSections = !!c && c.sections.length > 0;
        const allSectionsHaveLessons =
            !!c && c.sections.length > 0 && c.sections.every((s) => s.lessons.length > 0);

        const items = [
            { label: t('publishChecklistCover'), ok: hasCover },
            { label: t('publishChecklistSections'), ok: hasSections },
            { label: t('publishChecklistLessons'), ok: allSectionsHaveLessons },
        ];

        const allDone = items.every((item) => item.ok);

        return (
            <div
                className={cn(
                    'rounded-xl border p-4',
                    allDone ? 'border-success/30 bg-success/10' : 'border-warning/30 bg-warning/10',
                )}
            >
                <p className={cn('mb-2 font-medium', allDone ? 'text-success' : 'text-warning')}>
                    {t('publishChecklistTitle')}
                </p>
                <ul className="space-y-1 text-sm text-muted-foreground">
                    {items.map(({ label, ok }) => (
                        <li key={label} className="flex items-center gap-2">
                            {ok ? (
                                <CheckCircle size={14} className="shrink-0 text-success" />
                            ) : (
                                <Circle size={14} className="shrink-0 opacity-60" />
                            )}
                            {label}
                        </li>
                    ))}
                </ul>
            </div>
        );
    }

    // The counts the checklist reasons about, spelled out, alongside the figures an instructor
    // would otherwise leave the editor to look up.
    function renderCourseFacts(c: CourseForEditDto) {
        const lessonsCount = c.sections.reduce((sum, s) => sum + s.lessons.length, 0);
        const statusLabels: Record<CourseStatus, string> = {
            Published: t('common:status.published'),
            Draft: t('common:status.draft'),
            Archived: t('common:status.archived'),
        };

        const rows = [
            { label: t('common:status.status'), value: statusLabels[c.status] },
            { label: t('colStudents'), value: c.enrollmentsCount },
            { label: t('editorFactsSections'), value: c.sections.length },
            { label: t('editorFactsLessons'), value: lessonsCount },
            {
                label: t('editorFactsUpdated'),
                value: new Date(c.updatedAt).toLocaleDateString(i18n.language),
            },
        ];

        return (
            <div className="rounded-xl border border-border bg-card p-4">
                <p className="mb-3 font-medium text-foreground">{t('editorFactsTitle')}</p>
                <dl className="space-y-2 text-sm">
                    {rows.map(({ label, value }) => (
                        <div key={label} className="flex items-baseline justify-between gap-3">
                            <dt className="text-muted-foreground">{label}</dt>
                            <dd className="font-medium text-foreground">{value}</dd>
                        </div>
                    ))}
                </dl>
                {/* Only a published course has a public page to open, and it opens in its own tab:
                    this one holds an editor that may have unsaved edits in it. */}
                {c.status === 'Published' && (
                    <a
                        href={APP_ROUTES.public.courseDetail(c.id)}
                        target="_blank"
                        rel="noreferrer"
                        className="mt-4 flex items-center gap-1.5 border-t border-border pt-3 text-sm text-link hover:underline"
                    >
                        <ExternalLink size={14} />
                        {t('editorViewPublicPage')}
                    </a>
                )}
            </div>
        );
    }

    return (
        <div className="flex min-h-full flex-col">
            {/* One toolbar row, edge to edge: the course name pinned to the left margin, its actions
                to the right one. The tabs sit with the name because they navigate within this
                course; the centre of the bar belongs to global navigation. Sticky, so Save stays
                reachable while the form scrolls. */}
            <header className="sticky top-0 z-20 border-b border-border bg-card">
                <div className="flex min-h-14 flex-wrap items-center gap-x-4 gap-y-2 px-6 py-2 md:flex-nowrap md:py-0">
                    <h1 className="min-w-0 truncate font-heading font-semibold text-foreground">
                        {title}
                    </h1>

                    <div className="hidden h-5 w-px shrink-0 bg-border md:block" />

                    <nav className="flex shrink-0 items-center gap-1 rounded-lg bg-secondary p-1 text-sm">
                        {(['info', 'curriculum'] as Tab[]).map((tabKey) => (
                            <button
                                key={tabKey}
                                type="button"
                                onClick={() => setTab(tabKey)}
                                className={cn(
                                    'rounded-md px-3 py-1.5 transition-colors',
                                    tab === tabKey
                                        ? 'bg-primary/10 font-medium text-primary'
                                        : 'text-muted-foreground hover:text-foreground',
                                )}
                            >
                                {tabKey === 'info' ? t('tabInfo') : t('tabCurriculum')}
                            </button>
                        ))}
                    </nav>

                    <div className="ml-auto flex shrink-0 items-center gap-2">
                        {!isNew && course && (
                            <Button variant="outline" onClick={() => setShowPreview(true)}>
                                <Eye size={14} />
                                {t('preview.button')}
                            </Button>
                        )}
                        {tab === 'info' && (
                            <AsyncButton
                                type="submit"
                                form="course-info-form"
                                variant="success"
                                // Nothing to send until the form holds something the course does not.
                                disabled={!isInfoDirty}
                                isLoading={isSaving}
                                loadingText={t('common:actions.saving')}
                            >
                                {t('common:actions.save')}
                            </AsyncButton>
                        )}
                        {!isNew && isArchived && (
                            <AsyncButton
                                variant="outline"
                                onClick={() => unarchiveCourse.mutate(id!)}
                                isLoading={unarchiveCourse.isPending}
                                loadingText={t('common:actions.submitting')}
                            >
                                <ArchiveRestore size={14} />
                                {t('btnUnarchiveCourse')}
                            </AsyncButton>
                        )}
                        {!isNew && course && !isArchived && (
                            <>
                                {isPublished ? (
                                    <AsyncButton
                                        variant="warning"
                                        onClick={() => setShowUnpublishConfirm(true)}
                                        isLoading={unpublishCourse.isPending}
                                        loadingText={t('common:actions.submitting')}
                                    >
                                        {t('common:actions.unpublish')}
                                    </AsyncButton>
                                ) : (
                                    <AsyncButton
                                        variant="success"
                                        onClick={() => setShowPublishConfirm(true)}
                                        isLoading={publishCourse.isPending}
                                        loadingText={t('common:actions.submitting')}
                                    >
                                        {t('common:actions.publish')}
                                    </AsyncButton>
                                )}
                            </>
                        )}
                    </div>
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
                    {t('common:status.loading')}
                </div>
            ) : (
                /* One grid for every state — both tabs, saved or not — so the working panel holds
                   the same width and the checklist the same corner throughout. The panel takes
                   whatever height the viewport leaves it. */
                <div className="flex w-full flex-1 flex-col p-6">
                    <div className="grid flex-1 gap-6 xl:grid-cols-[minmax(0,1fr)_320px]">
                        <div className="flex min-w-0 flex-col rounded-xl border border-border bg-card p-6">
                            {tab === 'info' && (
                                <CourseInfoForm
                                    course={course}
                                    onSubmit={handleInfoSubmit}
                                    onDirtyChange={setIsInfoDirty}
                                />
                            )}

                            {tab === 'curriculum' &&
                                (id && course ? (
                                    <CurriculumTab courseId={id} sections={course.sections} />
                                ) : (
                                    <p className="m-auto text-sm text-muted-foreground">
                                        {t('curriculumNeedsSave')}
                                    </p>
                                ))}
                        </div>

                        {/* Above the panel while the layout is a single column — what the course
                            still needs is read before the form, not after scrolling past it. Beside
                            the panel it sits below the sticky header and follows the scroll. */}
                        <aside className="order-first space-y-4 xl:sticky xl:top-20 xl:order-none xl:self-start">
                            {renderPublishChecklist(course)}
                            {course && renderCourseFacts(course)}
                        </aside>
                    </div>
                </div>
            )}

            {showPublishConfirm && (
                <ConfirmDialog
                    title={t('confirmPublishTitle')}
                    description={t('confirmPublishDesc')}
                    confirmLabel={t('common:actions.publish')}
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
                    confirmLabel={t('common:actions.unpublish')}
                    variant="destructive"
                    onConfirm={() => {
                        unpublishCourse.mutate(id!);
                        setShowUnpublishConfirm(false);
                    }}
                    onClose={() => setShowUnpublishConfirm(false)}
                />
            )}

            {showPreview && course && (
                <CoursePreviewModal course={course} onClose={() => setShowPreview(false)} />
            )}
        </div>
    );
}
