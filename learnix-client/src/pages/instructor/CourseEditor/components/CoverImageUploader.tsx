import { useRef, useState } from 'react';
import { ImagePlus } from 'lucide-react';
import { cn } from '@/utils/cn';
import { useRequestUploadUrl } from '@/hooks/useRequestUploadUrl';
import { INSTRUCTOR } from '@/const/localization/instructor';

interface Props {
    value: string | null;
    onChange: (blobPath: string) => void;
}

export function CoverImageUploader({ value, onChange }: Props) {
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
                {INSTRUCTOR.COVER_IMAGE_LABEL}
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
                    <img src={displayUrl} alt="" className="h-full w-full object-cover" />
                ) : (
                    <div className="flex h-full flex-col items-center justify-center gap-2 text-muted-foreground">
                        <ImagePlus size={32} className="opacity-50" />
                        <span className="text-sm">
                            {isUploading
                                ? INSTRUCTOR.COVER_IMAGE_UPLOADING
                                : value
                                  ? INSTRUCTOR.COVER_IMAGE_REPLACE
                                  : INSTRUCTOR.COVER_IMAGE_HINT}
                        </span>
                    </div>
                )}
                {isUploading && (
                    <div className="absolute inset-0 flex items-center justify-center bg-background/60">
                        <span className="text-sm text-muted-foreground">
                            {INSTRUCTOR.COVER_IMAGE_UPLOADING}
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
