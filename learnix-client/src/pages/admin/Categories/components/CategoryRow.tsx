import { useTranslation } from 'react-i18next';
import { Check, Pencil, ShieldCheck, Trash2, X } from 'lucide-react';
import type { AdminCategoryListItemDto } from '@/api/categories.api';
import { ThumbnailCell } from './ThumbnailCell';

type FormState = {
    name: string;
    slug: string;
    blobPath?: string;
    previewUrl?: string;
    removeImage?: boolean;
};

const inputCls =
    'w-full rounded border border-input bg-background px-2 py-1 text-sm focus:outline-none focus:ring-1 focus:ring-ring';

interface CategoryRowProps {
    category: AdminCategoryListItemDto;
    isEditing: boolean;
    editForm: FormState;
    isCreating: boolean;
    updatePending: boolean;
    onEditChange: (form: FormState) => void;
    onStartEdit: () => void;
    onCancelEdit: () => void;
    onSaveEdit: () => void;
    onDeleteClick: () => void;
}

function nameToSlug(name: string): string {
    return name
        .toLowerCase()
        .trim()
        .replace(/\s+/g, '-')
        .replace(/[^a-z0-9-]/g, '')
        .replace(/-+/g, '-');
}

export function CategoryRow({
    category,
    isEditing,
    editForm,
    isCreating,
    updatePending,
    onEditChange,
    onStartEdit,
    onCancelEdit,
    onSaveEdit,
    onDeleteClick,
}: CategoryRowProps) {
    const { t } = useTranslation('admin');

    const handleEditNameChange = (value: string) => {
        onEditChange({ ...editForm, name: value, slug: nameToSlug(value) });
    };

    return (
        <tr className="hover:bg-secondary/30">
            {/* Image */}
            <td className="px-5 py-3">
                <ThumbnailCell
                    imageUrl={isEditing && editForm.removeImage ? null : category.imageUrl}
                    previewUrl={isEditing ? editForm.previewUrl : undefined}
                    isEditing={isEditing}
                    slug={isEditing ? editForm.slug : category.slug}
                    onUpload={(blobPath, previewUrl) =>
                        onEditChange({
                            ...editForm,
                            blobPath,
                            previewUrl,
                            removeImage: false,
                        })
                    }
                    onRemoveImage={() =>
                        onEditChange({
                            ...editForm,
                            blobPath: undefined,
                            previewUrl: undefined,
                            removeImage: true,
                        })
                    }
                />
            </td>

            {/* Name */}
            <td className="px-5 py-3">
                {isEditing ? (
                    <input
                        className={inputCls}
                        value={editForm.name}
                        onChange={(e) => handleEditNameChange(e.target.value)}
                        autoFocus
                    />
                ) : (
                    <div className="flex items-center gap-2">
                        <span className="font-medium text-foreground">{category.name}</span>
                        {category.isSystem && (
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
                {isEditing ? (
                    <input
                        className={inputCls}
                        value={editForm.slug}
                        onChange={(e) => onEditChange({ ...editForm, slug: e.target.value })}
                    />
                ) : (
                    <span className="font-mono text-xs text-muted-foreground">{category.slug}</span>
                )}
            </td>

            {/* Courses Count */}
            <td className="px-5 py-3 text-muted-foreground">{category.coursesCount}</td>

            {/* Actions */}
            <td className="px-5 py-3">
                <div className="flex items-center justify-end gap-1">
                    {isEditing ? (
                        <>
                            <button
                                onClick={onSaveEdit}
                                disabled={!editForm.name || !editForm.slug || updatePending}
                                className="rounded p-1.5 text-success transition-colors hover:bg-secondary disabled:opacity-40"
                                title={t('btnSave')}
                            >
                                <Check size={14} />
                            </button>
                            <button
                                onClick={onCancelEdit}
                                className="rounded p-1.5 text-muted-foreground transition-colors hover:bg-secondary"
                                title={t('btnCancel')}
                            >
                                <X size={14} />
                            </button>
                        </>
                    ) : (
                        <>
                            <button
                                onClick={onStartEdit}
                                disabled={isCreating || isEditing}
                                className="rounded p-1.5 text-muted-foreground transition-colors hover:bg-secondary hover:text-foreground disabled:opacity-40"
                                title={t('btnEditCategory')}
                            >
                                <Pencil size={14} />
                            </button>
                            <button
                                onClick={onDeleteClick}
                                disabled={isCreating || isEditing || category.isSystem}
                                className="rounded p-1.5 text-muted-foreground transition-colors hover:bg-secondary hover:text-destructive disabled:cursor-not-allowed disabled:opacity-30"
                                title={
                                    category.isSystem
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
    );
}
