import { useTranslation } from 'react-i18next';
import { Check, Pencil, ShieldCheck, Trash2, X } from 'lucide-react';
import type { AdminCategoryListItemDto } from '@/api/categories.api';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { TableCell, TableRow } from '@/components/ui/table';
import { ThumbnailCell } from './ThumbnailCell';

type FormState = {
    name: string;
    slug: string;
    blobPath?: string;
    previewUrl?: string;
    removeImage?: boolean;
};

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
        <TableRow className="hover:bg-secondary/30">
            {/* Image */}
            <TableCell className="px-5 py-3">
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
            </TableCell>

            {/* Name */}
            <TableCell className="px-5 py-3">
                {isEditing ? (
                    <Input
                        value={editForm.name}
                        onChange={(e: React.ChangeEvent<HTMLInputElement>) =>
                            handleEditNameChange(e.target.value)
                        }
                        autoFocus
                        className="h-8 w-full"
                    />
                ) : (
                    <div className="flex flex-col items-start gap-1.5">
                        <span className="font-medium text-foreground">{category.name}</span>
                        {category.isSystem && (
                            <Badge variant="secondary" className="gap-1 px-2 py-0 text-[10px]">
                                <ShieldCheck size={10} />
                                {t('categorySystemBadge')}
                            </Badge>
                        )}
                    </div>
                )}
            </TableCell>

            {/* Slug */}
            <TableCell className="px-5 py-3">
                {isEditing ? (
                    <Input
                        value={editForm.slug}
                        onChange={(e: React.ChangeEvent<HTMLInputElement>) =>
                            onEditChange({ ...editForm, slug: e.target.value })
                        }
                        className="h-8 w-full"
                    />
                ) : (
                    <span className="font-mono text-xs text-muted-foreground">{category.slug}</span>
                )}
            </TableCell>

            {/* Courses Count */}
            <TableCell className="px-5 py-3 text-muted-foreground">
                {category.coursesCount}
            </TableCell>

            {/* Actions */}
            <TableCell className="px-5 py-3">
                <div className="flex items-center justify-end gap-1">
                    {isEditing ? (
                        <>
                            <Button
                                variant="ghost"
                                size="icon"
                                onClick={onSaveEdit}
                                disabled={!editForm.name || !editForm.slug || updatePending}
                                className="size-8 text-success hover:bg-success/10 hover:text-success disabled:opacity-40"
                                title={t('common:actions.save')}
                            >
                                <Check size={14} />
                            </Button>
                            <Button
                                variant="ghost"
                                size="icon"
                                onClick={onCancelEdit}
                                className="size-8 text-muted-foreground hover:bg-secondary"
                                title={t('common:actions.cancel')}
                            >
                                <X size={14} />
                            </Button>
                        </>
                    ) : (
                        <>
                            <Button
                                variant="ghost"
                                size="icon"
                                onClick={onStartEdit}
                                disabled={isCreating || isEditing}
                                className="size-8 text-muted-foreground hover:bg-secondary hover:text-foreground disabled:opacity-40"
                                title={t('common:actions.edit')}
                            >
                                <Pencil size={14} />
                            </Button>
                            <Button
                                variant="ghost"
                                size="icon"
                                onClick={onDeleteClick}
                                disabled={isCreating || isEditing || category.isSystem}
                                className="size-8 text-muted-foreground hover:bg-destructive/10 hover:text-destructive disabled:cursor-not-allowed disabled:opacity-30"
                                title={
                                    category.isSystem
                                        ? t('categorySystemCannotDelete')
                                        : t('common:actions.delete')
                                }
                            >
                                <Trash2 size={14} />
                            </Button>
                        </>
                    )}
                </div>
            </TableCell>
        </TableRow>
    );
}
