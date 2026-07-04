import { useTranslation } from 'react-i18next';
import {
    DndContext,
    type DragEndEvent,
    PointerSensor,
    closestCenter,
    useSensor,
    useSensors,
} from '@dnd-kit/core';
import { SortableContext, arrayMove, verticalListSortingStrategy } from '@dnd-kit/sortable';
import { useCreateSection, useReorderSections } from '@/hooks/instructor/useSectionMutations';
import type { CourseForEditSectionDto } from '@/types/course.types';
import { SectionItem } from './SectionItem';

interface Props {
    courseId: string;
    sections: CourseForEditSectionDto[];
}

export function CurriculumTab({ courseId, sections }: Props) {
    const { t } = useTranslation('instructor');
    const createSection = useCreateSection(courseId);
    const reorderSections = useReorderSections(courseId);

    const sensors = useSensors(useSensor(PointerSensor, { activationConstraint: { distance: 5 } }));

    const sorted = [...sections].sort((a, b) => a.order - b.order);

    function handleDragEnd(event: DragEndEvent) {
        const { active, over } = event;
        if (!over || active.id === over.id) return;
        const oldIdx = sorted.findIndex((s) => s.id === active.id);
        const newIdx = sorted.findIndex((s) => s.id === over.id);
        const reordered = arrayMove(sorted, oldIdx, newIdx);
        reorderSections.mutate(reordered.map((s, i) => ({ id: s.id, order: i + 1 })));
    }

    return (
        <div className="space-y-3">
            <div className="flex items-center justify-between">
                <h3 className="font-heading font-semibold text-foreground">Curriculum</h3>
                <button
                    onClick={() => createSection.mutate('New section')}
                    disabled={createSection.isPending}
                    className="text-sm text-primary hover:underline disabled:opacity-60"
                >
                    {t('btnAddSection')}
                </button>
            </div>

            {sorted.length === 0 ? (
                <div className="rounded-lg border border-dashed border-border py-12 text-center text-sm text-muted-foreground">
                    No sections yet. Add one to start building your curriculum.
                </div>
            ) : (
                <DndContext
                    sensors={sensors}
                    collisionDetection={closestCenter}
                    onDragEnd={handleDragEnd}
                >
                    <SortableContext
                        items={sorted.map((s) => s.id)}
                        strategy={verticalListSortingStrategy}
                    >
                        <div className="space-y-3">
                            {sorted.map((section) => (
                                <SectionItem
                                    key={section.id}
                                    courseId={courseId}
                                    section={section}
                                />
                            ))}
                        </div>
                    </SortableContext>
                </DndContext>
            )}
        </div>
    );
}
