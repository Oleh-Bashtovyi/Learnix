import { useTranslation } from 'react-i18next';
import { Check, X } from 'lucide-react';
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

interface CategoryCreateRowProps {
    form: FormState;
    isPending: boolean;
    onChange: (form: FormState) => void;
    onSave: () => void;
    onCancel: () => void;
}

function nameToSlug(name: string): string {
    return name
        .toLowerCase()
        .trim()
        .replace(/\s+/g, '-')
        .replace(/[^a-z0-9-]/g, '')
        .replace(/-+/g, '-');
}

export function CategoryCreateRow({
    form,
    isPending,
    onChange,
    onSave,
    onCancel,
}: CategoryCreateRowProps) {
    const { t } = useTranslation('admin');

    const handleNameChange = (value: string) => {
        onChange({ ...form, name: value, slug: nameToSlug(value) });
    };

    return (
        <TableRow className="bg-primary/5 hover:bg-primary/5">
            <TableCell className="px-5 py-3">
                <ThumbnailCell
                    imageUrl={null}
                    previewUrl={form.previewUrl}
                    isEditing={true}
                    slug={form.slug}
                    onUpload={(blobPath, previewUrl) =>
                        onChange({
                            ...form,
                            blobPath,
                            previewUrl,
                            removeImage: false,
                        })
                    }
                    onRemoveImage={() =>
                        onChange({
                            ...form,
                            blobPath: undefined,
                            previewUrl: undefined,
                        })
                    }
                />
            </TableCell>
            <TableCell className="px-5 py-3">
                <Input
                    placeholder={t('categoryNamePlaceholder')}
                    value={form.name}
                    onChange={(e: React.ChangeEvent<HTMLInputElement>) =>
                        handleNameChange(e.target.value)
                    }
                    autoFocus
                    className="h-8 w-full"
                />
            </TableCell>
            <TableCell className="px-5 py-3">
                <Input
                    placeholder={t('categorySlugPlaceholder')}
                    value={form.slug}
                    onChange={(e: React.ChangeEvent<HTMLInputElement>) =>
                        onChange({ ...form, slug: e.target.value })
                    }
                    className="h-8 w-full"
                />
            </TableCell>
            <TableCell className="px-5 py-3 text-muted-foreground">-</TableCell>
            <TableCell className="px-5 py-3">
                <div className="flex items-center justify-end gap-1">
                    <Button
                        variant="ghost"
                        size="icon"
                        onClick={onSave}
                        disabled={!form.name || !form.slug || isPending}
                        className="size-8 text-success hover:bg-success/10 hover:text-success disabled:opacity-40"
                        title={t('btnSave')}
                    >
                        <Check size={14} />
                    </Button>
                    <Button
                        variant="ghost"
                        size="icon"
                        onClick={onCancel}
                        className="size-8 text-muted-foreground hover:bg-secondary"
                        title={t('btnCancel')}
                    >
                        <X size={14} />
                    </Button>
                </div>
            </TableCell>
        </TableRow>
    );
}
