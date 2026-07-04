import { useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import {
    DndContext,
    type DragEndEvent,
    PointerSensor,
    closestCenter,
    useSensor,
    useSensors,
} from '@dnd-kit/core';
import {
    SortableContext,
    arrayMove,
    useSortable,
    verticalListSortingStrategy,
} from '@dnd-kit/sortable';
import { CSS } from '@dnd-kit/utilities';
import { GripVertical, Trash2 } from 'lucide-react';
import { ConfirmDialog } from '@/components/common/ui/ConfirmDialog';
import { LessonType } from '@/enums/lesson.enums';
import {
    useDeleteLesson,
    useReorderLessons as useReorderLessonsMutation,
    useToggleLessonVisibility,
} from '@/hooks/instructor/useLessonMutations';
import { useDeleteSection, useUpdateSectionTitle } from '@/hooks/instructor/useSectionMutations';
import type { CourseForEditLessonDto, CourseForEditSectionDto } from '@/types/course.types';
import { LessonEditorModal } from './LessonEditorModal';
import { LessonRow } from './LessonRow';

interface Props {
    courseId: string;
    section: CourseForEditSectionDto;
}

type ModalState = { type: LessonType; lesson?: CourseForEditLessonDto } | null;

export function SectionItem({ courseId, section }: Props) {
    const { t } = useTranslation('instructor');
    const { attributes, listeners, setNodeRef, transform, transition, isDragging } = useSortable({
        id: section.id,
    });

    const style = {
        transform: CSS.Transform.toString(transform),
        transition,
        opacity: isDragging ? 0.4 : 1,
    };

    const [modal, setModal] = useState<ModalState>(null);
    const [pendingDelete, setPendingDelete] = useState<{ id: string; title: string } | null>(null);
    const titleRef = useRef<HTMLInputElement>(null);

    const deleteSection = useDeleteSection(courseId);
    const updateTitle = useUpdateSectionTitle(courseId);
    const deleteLesson = useDeleteLesson(courseId);
    const reorderLessons = useReorderLessonsMutation(courseId, section.id);
    const toggleVisibility = useToggleLessonVisibility(courseId);

    const sensors = useSensors(useSensor(PointerSensor, { activationConstraint: { distance: 5 } }));

    function handleTitleBlur() {
        const newTitle = titleRef.current?.value.trim();
        if (newTitle && newTitle !== section.title) {
            updateTitle.mutate({ sectionId: section.id, title: newTitle });
        }
    }

    function handleDeleteSection() {
        if (!confirm(`Delete section "${section.title}" and all its lessons?`)) return;
        deleteSection.mutate(section.id);
    }

    function handleDeleteLesson(lessonId: string, title: string) {
        setPendingDelete({ id: lessonId, title });
    }

    function confirmDeleteLesson() {
        if (!pendingDelete) return;
        deleteLesson.mutate(pendingDelete.id, { onSettled: () => setPendingDelete(null) });
    }

    function handleLessonDragEnd(event: DragEndEvent) {
        const { active, over } = event;
        if (!over || active.id === over.id) return;
        const lessons = [...section.lessons].sort((a, b) => a.order - b.order);
        const oldIdx = lessons.findIndex((l) => l.id === active.id);
        const newIdx = lessons.findIndex((l) => l.id === over.id);
        const reordered = arrayMove(lessons, oldIdx, newIdx);
        reorderLessons.mutate(reordered.map((l, i) => ({ id: l.id, order: i + 1 })));
    }

    const sortedLessons = [...section.lessons].sort((a, b) => a.order - b.order);

    return (
        <>
            <div
                ref={setNodeRef}
                style={style}
                className="overflow-hidden rounded-lg border border-border"
            >
                {/* Section header */}
                <div className="flex items-center gap-3 bg-secondary/50 px-3 py-2.5">
                    <button
                        {...attributes}
                        {...listeners}
                        className="cursor-grab text-muted-foreground active:cursor-grabbing"
                    >
                        <GripVertical size={14} />
                    </button>
                    <input
                        ref={titleRef}
                        defaultValue={section.title}
                        onBlur={handleTitleBlur}
                        className="flex-1 rounded border border-transparent bg-transparent px-2 py-1 text-sm font-medium hover:border-border focus:border-border focus:outline-none"
                    />
                    <span className="shrink-0 text-xs text-muted-foreground">
                        {t('lessonCount', { count: section.lessons.length })}
                    </span>
                    <button
                        onClick={handleDeleteSection}
                        className="text-muted-foreground transition-colors hover:text-destructive"
                    >
                        <Trash2 size={14} />
                    </button>
                </div>

                {/* Lessons */}
                <DndContext
                    sensors={sensors}
                    collisionDetection={closestCenter}
                    onDragEnd={handleLessonDragEnd}
                >
                    <SortableContext
                        items={sortedLessons.map((l) => l.id)}
                        strategy={verticalListSortingStrategy}
                    >
                        {sortedLessons.map((lesson) => (
                            <LessonRow
                                key={lesson.id}
                                lesson={lesson}
                                onEdit={() => setModal({ type: lesson.lessonType, lesson })}
                                onDelete={() => handleDeleteLesson(lesson.id, lesson.title)}
                                onToggleVisibility={() =>
                                    toggleVisibility.mutate({
                                        lessonId: lesson.id,
                                        isVisible: lesson.isHidden,
                                    })
                                }
                            />
                        ))}
                    </SortableContext>
                </DndContext>

                {/* Add lesson buttons */}
                <div className="flex gap-2 border-t border-border px-3 py-2">
                    {(['Video', 'Post', 'Test'] as LessonType[]).map((type) => (
                        <button
                            key={type}
                            onClick={() => setModal({ type })}
                            className="rounded border border-dashed border-border px-3 py-1 text-xs text-muted-foreground transition-colors hover:border-primary hover:text-primary"
                        >
                            {type === 'Video'
                                ? t('btnAddVideo')
                                : type === 'Post'
                                  ? t('btnAddPost')
                                  : t('btnAddTest')}
                        </button>
                    ))}
                </div>
            </div>

            {modal && (
                <LessonEditorModal
                    courseId={courseId}
                    sectionId={section.id}
                    lessonType={modal.type}
                    lesson={modal.lesson}
                    onClose={() => setModal(null)}
                />
            )}

            {pendingDelete && (
                <ConfirmDialog
                    title={t('btnDelete')}
                    description={t('confirmDeleteLesson', { title: pendingDelete.title })}
                    confirmLabel={t('btnDelete')}
                    variant="destructive"
                    isPending={deleteLesson.isPending}
                    onConfirm={confirmDeleteLesson}
                    onClose={() => setPendingDelete(null)}
                />
            )}
        </>
    );
}
