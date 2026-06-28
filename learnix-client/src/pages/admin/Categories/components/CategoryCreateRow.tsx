import { useTranslation } from 'react-i18next';
import { Check, X } from 'lucide-react';
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
        <tr className="bg-primary/5">
            <td className="px-5 py-3">
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
            </td>
            <td className="px-5 py-3">
                <input
                    className={inputCls}
                    placeholder={t('categoryNamePlaceholder')}
                    value={form.name}
                    onChange={(e) => handleNameChange(e.target.value)}
                    autoFocus
                />
            </td>
            <td className="px-5 py-3">
                <input
                    className={inputCls}
                    placeholder={t('categorySlugPlaceholder')}
                    value={form.slug}
                    onChange={(e) => onChange({ ...form, slug: e.target.value })}
                />
            </td>
            <td className="px-5 py-3 text-muted-foreground">-</td>
            <td className="px-5 py-3">
                <div className="flex items-center justify-end gap-1">
                    <button
                        onClick={onSave}
                        disabled={!form.name || !form.slug || isPending}
                        className="rounded p-1.5 text-success transition-colors hover:bg-secondary disabled:opacity-40"
                        title={t('btnSave')}
                    >
                        <Check size={14} />
                    </button>
                    <button
                        onClick={onCancel}
                        className="rounded p-1.5 text-muted-foreground transition-colors hover:bg-secondary"
                        title={t('btnCancel')}
                    >
                        <X size={14} />
                    </button>
                </div>
            </td>
        </tr>
    );
}
