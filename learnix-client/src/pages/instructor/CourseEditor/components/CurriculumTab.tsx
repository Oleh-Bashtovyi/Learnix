import { useState } from 'react';
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
import { AsyncButton } from '@/components/ui/async-button';
import { Button } from '@/components/ui/button';
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

    // Collapsed sections live here, not in SectionItem, so the header's Collapse/Expand all has a
    // single place to set: reordering a long curriculum is done with every section folded away.
    const [collapsedIds, setCollapsedIds] = useState<Set<string>>(new Set());

    function toggleCollapsed(sectionId: string) {
        setCollapsedIds((prev) => {
            const next = new Set(prev);
            if (!next.delete(sectionId)) next.add(sectionId);
            return next;
        });
    }

    const allCollapsed = sections.length > 0 && collapsedIds.size >= sections.length;

    function toggleAll() {
        setCollapsedIds(allCollapsed ? new Set() : new Set(sections.map((s) => s.id)));
    }

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
                <h3 className="font-heading font-semibold text-foreground">{t('tabCurriculum')}</h3>
                <div className="flex items-center gap-4">
                    {sections.length > 0 && (
                        <Button variant="link" onClick={toggleAll} className="h-auto p-0">
                            {allCollapsed ? t('btnExpandAll') : t('btnCollapseAll')}
                        </Button>
                    )}
                    <AsyncButton
                        variant="link"
                        onClick={() => createSection.mutate(t('defaultSectionTitle'))}
                        isLoading={createSection.isPending}
                        loadingText={t('common:actions.submitting')}
                        className="h-auto p-0"
                    >
                        {t('btnAddSection')}
                    </AsyncButton>
                </div>
            </div>

            {sorted.length === 0 ? (
                <div className="rounded-lg border border-dashed border-border py-12 text-center text-sm text-muted-foreground">
                    {t('curriculumEmpty')}
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
                                    isCollapsed={collapsedIds.has(section.id)}
                                    onToggleCollapse={() => toggleCollapsed(section.id)}
                                />
                            ))}
                        </div>
                    </SortableContext>
                </DndContext>
            )}
        </div>
    );
}
