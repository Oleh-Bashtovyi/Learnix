import { useState } from 'react';
import { useRef } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
    Pencil,
    Trash2,
    Check,
    X,
    Plus,
    FolderOpen,
    ShieldCheck,
    Upload,
    Loader2,
} from 'lucide-react';
import { toast } from 'sonner';
import { useTranslation } from 'react-i18next';
import { categoriesApi } from '@/api/categories.api';
import { queryKeys } from '@/api/queryKeys';
import { ConfirmDialog } from '@/components/common/ConfirmDialog';
import { useRequestUploadUrl } from '@/hooks/useRequestUploadUrl';
import { getCategoryVisuals } from '@/mocks/landing.mock';
import { cn } from '@/utils/cn';
import type { AdminCategoryListItemDto } from '@/api/categories.api';

type FormState = { name: string; slug: string; blobPath?: string; previewUrl?: string; removeImage?: boolean };

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

function ThumbnailCell({
    imageUrl,
    isEditing,
    previewUrl,
    onUpload,
}: {
    imageUrl: string | null;
    isEditing: boolean;
    previewUrl?: string;
    slug: string;
    onUpload?: (blobPath: string, previewUrl: string) => void;
    onRemoveImage?: () => void;
}) {
    const { uploadFile, isUploading } = useRequestUploadUrl();
    const fileRef = useRef<HTMLInputElement>(null);

    const handleFile = async (e: React.ChangeEvent<HTMLInputElement>) => {
        const file = e.target.files?.[0];
        if (!file || !onUpload) return;
        try {
            const blobPath = await uploadFile('CategoryImage', file);
            onUpload(blobPath, URL.createObjectURL(file));
        } catch {
            toast.error('Failed to upload image');
        }
    };

    const displayUrl = previewUrl || imageUrl;
    const visual = getCategoryVisuals(slug);

    if (!isEditing) {
        return displayUrl ? (
            <img src={displayUrl} alt="" className="h-10 w-10 rounded object-cover" />
        ) : (
            <div className={cn('flex h-10 w-10 items-center justify-center rounded text-xl', visual.iconBgClass, visual.iconTextClass)}>
                {visual.emoji}
            </div>
        );
    }

    return (
        <div className="group relative flex h-10 w-10 items-center justify-center overflow-hidden rounded bg-muted hover:bg-muted/80">
            {isUploading ? (
                <Loader2 size={16} className="animate-spin text-muted-foreground" />
            ) : displayUrl ? (
                <>
                    <img src={displayUrl} alt="" className="h-full w-full object-cover" />
                    <div className="absolute inset-0 flex flex-col items-center justify-center bg-black/50 opacity-0 transition-opacity group-hover:opacity-100">
                        <button
                            type="button"
                            className="flex h-1/2 w-full items-center justify-center text-white hover:bg-white/20"
                            onClick={() => fileRef.current?.click()}
                            title="Upload new image"
                        >
                            <Upload size={14} />
                        </button>
                        <button
                            type="button"
                            className="flex h-1/2 w-full items-center justify-center text-destructive hover:bg-white/20"
                            onClick={(e) => {
                                e.stopPropagation();
                                onRemoveImage?.();
                            }}
                            title="Remove image"
                        >
                            <Trash2 size={14} />
                        </button>
                    </div>
                </>
            ) : (
                <div 
                    className={cn('flex h-10 w-10 cursor-pointer items-center justify-center rounded text-xl', visual.iconBgClass, visual.iconTextClass)}
                    onClick={() => fileRef.current?.click()}
                >
                    {visual.emoji}
                    <div className="absolute inset-0 flex items-center justify-center bg-black/50 opacity-0 transition-opacity group-hover:opacity-100">
                        <Upload size={14} className="text-white" />
                    </div>
                </div>
            )}
            <input
                type="file"
                ref={fileRef}
                className="hidden"
                accept="image/jpeg,image/png,image/webp"
                onChange={handleFile}
            />
        </div>
    );
}

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
        }
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
        }
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
                                        <ThumbnailCell
                                            imageUrl={null}
                                            previewUrl={createForm.previewUrl}
                                            isEditing={true}
                                            slug={createForm.slug}
                                            onUpload={(blobPath, previewUrl) =>
                                                setCreateForm((f) => ({
                                                    ...f,
                                                    blobPath,
                                                    previewUrl,
                                                    removeImage: false,
                                                }))
                                            }
                                            onRemoveImage={() =>
                                                setCreateForm((f) => ({
                                                    ...f,
                                                    blobPath: undefined,
                                                    previewUrl: undefined,
                                                }))
                                            }
                                        />
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
                                        <ThumbnailCell
                                            imageUrl={editingId === cat.id && editForm.removeImage ? null : cat.imageUrl}
                                            previewUrl={
                                                editingId === cat.id
                                                    ? editForm.previewUrl
                                                    : undefined
                                            }
                                            isEditing={editingId === cat.id}
                                            slug={editingId === cat.id ? editForm.slug : cat.slug}
                                            onUpload={(blobPath, previewUrl) =>
                                                setEditForm((f) => ({ ...f, blobPath, previewUrl, removeImage: false }))
                                            }
                                            onRemoveImage={() =>
                                                setEditForm((f) => ({
                                                    ...f,
                                                    blobPath: undefined,
                                                    previewUrl: undefined,
                                                    removeImage: true,
                                                }))
                                            }
                                        />
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
