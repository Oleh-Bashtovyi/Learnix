import { useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { ImagePlus } from 'lucide-react';
import { useRequestUploadUrl } from '@/hooks/shared/useRequestUploadUrl';
import { cn } from '@/utils/cn';

interface Props {
    value: string | null;
    onChange: (blobPath: string) => void;
}

export function CoverImageUploader({ value, onChange }: Props) {
    const { t } = useTranslation('instructor');
    const inputRef = useRef<HTMLInputElement>(null);
    const { uploadFile, isUploading, error } = useRequestUploadUrl();
    const [previewUrl, setPreviewUrl] = useState<string | null>(null);

    async function handleFile(file: File) {
        if (!file.type.startsWith('image/')) return;
        setPreviewUrl(URL.createObjectURL(file));
        try {
            const blobPath = await uploadFile('CourseCover', file);
            onChange(blobPath);
        } catch {
            setPreviewUrl(null);
        }
    }

    function onInputChange(e: React.ChangeEvent<HTMLInputElement>) {
        const file = e.target.files?.[0];
        if (file) handleFile(file);
    }

    function onDrop(e: React.DragEvent) {
        e.preventDefault();
        const file = e.dataTransfer.files?.[0];
        if (file) handleFile(file);
    }

    const displayUrl = previewUrl ?? value;

    return (
        <div className="space-y-1.5">
            <label className="block text-sm font-medium text-foreground">
                {t('coverImageLabel')}
            </label>
            <div
                onClick={() => !isUploading && inputRef.current?.click()}
                onDrop={onDrop}
                onDragOver={(e) => e.preventDefault()}
                className={cn(
                    'relative aspect-video cursor-pointer overflow-hidden rounded-lg border-2 border-dashed border-border bg-muted transition-colors hover:border-primary',
                    isUploading && 'cursor-wait opacity-70',
                )}
            >
                {displayUrl ? (
                    <img src={displayUrl} alt="" className="size-full object-cover" />
                ) : (
                    <div className="flex h-full flex-col items-center justify-center gap-2 text-muted-foreground">
                        <ImagePlus size={32} className="opacity-50" />
                        <span className="text-sm">
                            {isUploading
                                ? t('coverImageUploading')
                                : value
                                  ? t('coverImageReplace')
                                  : t('coverImageHint')}
                        </span>
                    </div>
                )}
                {isUploading && (
                    <div className="absolute inset-0 flex items-center justify-center bg-background/60">
                        <span className="text-sm text-muted-foreground">
                            {t('coverImageUploading')}
                        </span>
                    </div>
                )}
            </div>
            {error && <p className="text-xs text-destructive">{error}</p>}
            <input
                ref={inputRef}
                type="file"
                accept="image/jpeg,image/png,image/webp"
                className="hidden"
                onChange={onInputChange}
            />
        </div>
    );
}
