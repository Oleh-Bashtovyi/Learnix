import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Plus } from 'lucide-react';
import { toast } from 'sonner';
import { categoriesApi } from '@/api/categories.api';
import type { AdminCategoryListItemDto } from '@/api/categories.api';
import { queryKeys } from '@/api/queryKeys';
import { ConfirmDialog } from '@/components/common/ConfirmDialog';
import { CategoryCreateRow } from './components/CategoryCreateRow';
import { CategoryRow } from './components/CategoryRow';

type FormState = {
    name: string;
    slug: string;
    blobPath?: string;
    previewUrl?: string;
    removeImage?: boolean;
};

export default function CategoryManagementPage() {
    const { t } = useTranslation('admin');
    const qc = useQueryClient();

    const [creating, setCreating] = useState(false);
    const [createForm, setCreateForm] = useState<FormState>({ name: '', slug: '' });
    const [editingId, setEditingId] = useState<string | null>(null);
    const [editForm, setEditForm] = useState<FormState>({ name: '', slug: '' });
    const [pendingDelete, setPendingDelete] = useState<AdminCategoryListItemDto | null>(null);

    const { data: categories = [], isLoading } = useQuery({
        queryKey: queryKeys.categories.adminList(),
        queryFn: () => categoriesApi.getAllForAdmin(),
    });

    function invalidate() {
        qc.invalidateQueries({ queryKey: queryKeys.categories.all });
    }

    const createMutation = useMutation({
        mutationFn: async () => {
            const res = await categoriesApi.create({
                name: createForm.name,
                slug: createForm.slug,
            });
            if (createForm.blobPath) {
                await categoriesApi.setImage(res.id, createForm.blobPath);
            }
        },
        onSuccess: () => {
            toast.success(t('toastCategoryCreated'));
            setCreating(false);
            setCreateForm({ name: '', slug: '' });
            invalidate();
        },
        onError: () => {
            setCreateForm((f) => ({ ...f, blobPath: undefined, previewUrl: undefined }));
        },
    });

    const updateMutation = useMutation({
        mutationFn: async (id: string) => {
            await categoriesApi.update(id, { name: editForm.name, slug: editForm.slug });
            if (editForm.removeImage) {
                await categoriesApi.deleteImage(id);
            } else if (editForm.blobPath) {
                await categoriesApi.setImage(id, editForm.blobPath);
            }
        },
        onSuccess: () => {
            toast.success(t('toastCategoryUpdated'));
            setEditingId(null);
            invalidate();
        },
        onError: () => {
            setEditForm((f) => ({ ...f, blobPath: undefined, previewUrl: undefined }));
        },
    });

    const deleteMutation = useMutation({
        mutationFn: (id: string) => categoriesApi.delete(id),
        onSuccess: () => {
            toast.success(t('toastCategoryDeleted'));
            setPendingDelete(null);
            invalidate();
        },
    });

    function startCreate() {
        setEditingId(null);
        setCreating(true);
        setCreateForm({ name: '', slug: '' });
    }

    function startEdit(cat: AdminCategoryListItemDto) {
        setCreating(false);
        setEditingId(cat.id);
        setEditForm({ name: cat.name, slug: cat.slug });
    }

    function cancelEdit() {
        setEditingId(null);
        setCreating(false);
    }

    return (
        <div className="p-8">
            {/* Header */}
            <div className="mb-8 flex items-start justify-between">
                <div>
                    <h1 className="font-heading text-3xl font-bold text-foreground">
                        {t('categoriesTitle')}
                    </h1>
                    <p className="mt-1 text-muted-foreground">{t('categoriesSubtitle')}</p>
                </div>
                {!creating && (
                    <button
                        onClick={startCreate}
                        className="flex items-center gap-2 rounded-lg bg-primary px-4 py-2 text-sm font-medium text-primary-foreground transition-opacity hover:opacity-90"
                    >
                        <Plus size={16} />
                        {t('btnAddCategory')}
                    </button>
                )}
            </div>

            {/* Table */}
            <div className="overflow-hidden rounded-xl border border-border bg-card">
                {isLoading ? (
                    <div className="py-16 text-center text-sm text-muted-foreground">
                        Loading...
                    </div>
                ) : (
                    <table className="w-full text-sm">
                        <thead className="bg-secondary/50 text-xs uppercase tracking-wider text-muted-foreground">
                            <tr>
                                <th className="w-14 px-5 py-3 text-left font-medium">
                                    {t('colImage')}
                                </th>
                                <th className="px-5 py-3 text-left font-medium">{t('colName')}</th>
                                <th className="px-5 py-3 text-left font-medium">{t('colSlug')}</th>
                                <th className="px-5 py-3 text-left font-medium">
                                    {t('colCourses')}
                                </th>
                                <th className="w-24 px-5 py-3 text-right font-medium">
                                    {t('colActions')}
                                </th>
                            </tr>
                        </thead>
                        <tbody className="divide-y divide-border">
                            {/* Create row */}
                            {creating && (
                                <CategoryCreateRow
                                    form={createForm}
                                    isPending={createMutation.isPending}
                                    onChange={setCreateForm}
                                    onSave={() => createMutation.mutate()}
                                    onCancel={cancelEdit}
                                />
                            )}

                            {/* Empty state */}
                            {!creating && categories.length === 0 && (
                                <tr>
                                    <td
                                        colSpan={5}
                                        className="py-16 text-center text-sm text-muted-foreground"
                                    >
                                        {t('emptyCategories')}
                                    </td>
                                </tr>
                            )}

                            {/* Category rows */}
                            {categories.map((cat) => (
                                <CategoryRow
                                    key={cat.id}
                                    category={cat}
                                    isEditing={editingId === cat.id}
                                    editForm={editForm}
                                    isCreating={creating}
                                    updatePending={updateMutation.isPending}
                                    onEditChange={setEditForm}
                                    onStartEdit={() => startEdit(cat)}
                                    onCancelEdit={cancelEdit}
                                    onSaveEdit={() => updateMutation.mutate(cat.id)}
                                    onDeleteClick={() => setPendingDelete(cat)}
                                />
                            ))}
                        </tbody>
                    </table>
                )}
            </div>

            {/* Delete confirm */}
            {pendingDelete && (
                <ConfirmDialog
                    title={t('btnDeleteCategory')}
                    description={t('confirmDeleteCategory', { name: pendingDelete.name })}
                    confirmLabel={t('btnDeleteCategory')}
                    variant="destructive"
                    isPending={deleteMutation.isPending}
                    onConfirm={() => deleteMutation.mutate(pendingDelete.id)}
                    onClose={() => setPendingDelete(null)}
                />
            )}
        </div>
    );
}
