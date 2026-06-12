import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Pencil, Trash2, Check, X, Plus, FolderOpen, ShieldCheck } from 'lucide-react';
import { toast } from 'sonner';
import { useTranslation } from 'react-i18next';
import { categoriesApi } from '@/api/categories.api';
import { queryKeys } from '@/api/queryKeys';
import { ConfirmDialog } from '@/components/common/ConfirmDialog';
import type { AdminAdminCategoryListItemDto } from '@/api/categories.api';

type FormState = { name: string; slug: string };

function nameToSlug(name: string): string {
    return name
        .toLowerCase()
        .trim()
        .replace(/\s+/g, '-')
        .replace(/[^a-z0-9-]/g, '')
        .replace(/-+/g, '-');
}

const inputCls =
    'w-full rounded border border-input bg-background px-2 py-1 text-sm focus:outline-none focus:ring-1 focus:ring-ring';

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
        mutationFn: () => categoriesApi.create(createForm),
        onSuccess: () => {
            toast.success(t('toastCategoryCreated'));
            setCreating(false);
            setCreateForm({ name: '', slug: '' });
            invalidate();
        },
    });

    const updateMutation = useMutation({
        mutationFn: (id: string) => categoriesApi.update(id, editForm),
        onSuccess: () => {
            toast.success(t('toastCategoryUpdated'));
            setEditingId(null);
            invalidate();
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

    function handleCreateNameChange(value: string) {
        setCreateForm({ name: value, slug: nameToSlug(value) });
    }

    function handleEditNameChange(value: string) {
        setEditForm({ name: value, slug: nameToSlug(value) });
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
                                <tr className="bg-primary/5">
                                    <td className="px-5 py-3">
                                        <div className="h-10 w-10 rounded bg-muted" />
                                    </td>
                                    <td className="px-5 py-3">
                                        <input
                                            className={inputCls}
                                            placeholder={t('categoryNamePlaceholder')}
                                            value={createForm.name}
                                            onChange={(e) => handleCreateNameChange(e.target.value)}
                                            autoFocus
                                        />
                                    </td>
                                    <td className="px-5 py-3">
                                        <input
                                            className={inputCls}
                                            placeholder={t('categorySlugPlaceholder')}
                                            value={createForm.slug}
                                            onChange={(e) =>
                                                setCreateForm((f) => ({
                                                    ...f,
                                                    slug: e.target.value,
                                                }))
                                            }
                                        />
                                    </td>
                                    <td className="px-5 py-3 text-muted-foreground">-</td>
                                    <td className="px-5 py-3">
                                        <div className="flex items-center justify-end gap-1">
                                            <button
                                                onClick={() => createMutation.mutate()}
                                                disabled={
                                                    !createForm.name ||
                                                    !createForm.slug ||
                                                    createMutation.isPending
                                                }
                                                className="rounded p-1.5 text-success transition-colors hover:bg-secondary disabled:opacity-40"
                                                title={t('btnSave')}
                                            >
                                                <Check size={14} />
                                            </button>
                                            <button
                                                onClick={cancelEdit}
                                                className="rounded p-1.5 text-muted-foreground transition-colors hover:bg-secondary"
                                                title={t('btnCancel')}
                                            >
                                                <X size={14} />
                                            </button>
                                        </div>
                                    </td>
                                </tr>
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
                                <tr key={cat.id} className="hover:bg-secondary/30">
                                    {/* Image */}
                                    <td className="px-5 py-3">
                                        {cat.imageUrl ? (
                                            <img
                                                src={cat.imageUrl}
                                                alt=""
                                                className="h-10 w-10 rounded object-cover"
                                            />
                                        ) : (
                                            <div className="flex h-10 w-10 items-center justify-center rounded bg-muted">
                                                <FolderOpen
                                                    size={14}
                                                    className="text-muted-foreground/50"
                                                />
                                            </div>
                                        )}
                                    </td>

                                    {/* Name */}
                                    <td className="px-5 py-3">
                                        {editingId === cat.id ? (
                                            <input
                                                className={inputCls}
                                                value={editForm.name}
                                                onChange={(e) =>
                                                    handleEditNameChange(e.target.value)
                                                }
                                                autoFocus
                                            />
                                        ) : (
                                            <div className="flex items-center gap-2">
                                                <span className="font-medium text-foreground">
                                                    {cat.name}
                                                </span>
                                                {cat.isSystem && (
                                                    <span className="inline-flex items-center gap-1 rounded-full bg-muted px-2 py-0.5 text-xs text-muted-foreground">
                                                        <ShieldCheck size={10} />
                                                        {t('categorySystemBadge')}
                                                    </span>
                                                )}
                                            </div>
                                        )}
                                    </td>

                                    {/* Slug */}
                                    <td className="px-5 py-3">
                                        {editingId === cat.id ? (
                                            <input
                                                className={inputCls}
                                                value={editForm.slug}
                                                onChange={(e) =>
                                                    setEditForm((f) => ({
                                                        ...f,
                                                        slug: e.target.value,
                                                    }))
                                                }
                                            />
                                        ) : (
                                            <span className="font-mono text-xs text-muted-foreground">
                                                {cat.slug}
                                            </span>
                                        )}
                                    </td>

                                    {/* Courses Count */}
                                    <td className="px-5 py-3 text-muted-foreground">
                                        {cat.coursesCount}
                                    </td>

                                    {/* Actions */}
                                    <td className="px-5 py-3">
                                        <div className="flex items-center justify-end gap-1">
                                            {editingId === cat.id ? (
                                                <>
                                                    <button
                                                        onClick={() =>
                                                            updateMutation.mutate(cat.id)
                                                        }
                                                        disabled={
                                                            !editForm.name ||
                                                            !editForm.slug ||
                                                            updateMutation.isPending
                                                        }
                                                        className="rounded p-1.5 text-success transition-colors hover:bg-secondary disabled:opacity-40"
                                                        title={t('btnSave')}
                                                    >
                                                        <Check size={14} />
                                                    </button>
                                                    <button
                                                        onClick={cancelEdit}
                                                        className="rounded p-1.5 text-muted-foreground transition-colors hover:bg-secondary"
                                                        title={t('btnCancel')}
                                                    >
                                                        <X size={14} />
                                                    </button>
                                                </>
                                            ) : (
                                                <>
                                                    <button
                                                        onClick={() => startEdit(cat)}
                                                        disabled={creating || !!editingId}
                                                        className="rounded p-1.5 text-muted-foreground transition-colors hover:bg-secondary hover:text-foreground disabled:opacity-40"
                                                        title={t('btnEditCategory')}
                                                    >
                                                        <Pencil size={14} />
                                                    </button>
                                                    <button
                                                        onClick={() => setPendingDelete(cat)}
                                                        disabled={
                                                            creating || !!editingId || cat.isSystem
                                                        }
                                                        className="rounded p-1.5 text-muted-foreground transition-colors hover:bg-secondary hover:text-destructive disabled:cursor-not-allowed disabled:opacity-30"
                                                        title={
                                                            cat.isSystem
                                                                ? t('categorySystemCannotDelete')
                                                                : t('btnDeleteCategory')
                                                        }
                                                    >
                                                        <Trash2 size={14} />
                                                    </button>
                                                </>
                                            )}
                                        </div>
                                    </td>
                                </tr>
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
