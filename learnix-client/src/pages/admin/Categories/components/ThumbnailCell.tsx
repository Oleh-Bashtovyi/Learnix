import React, { useRef } from 'react';
import { Loader2, Trash2, Upload } from 'lucide-react';
import { toast } from 'sonner';
import { useRequestUploadUrl } from '@/hooks/shared/useRequestUploadUrl';
import { cn } from '@/utils/cn';
import { getCategoryVisuals } from '@/utils/mocks/landing.mock';

type ThumbnailCellProps = {
    imageUrl: string | null;
    isEditing: boolean;
    previewUrl?: string;
    slug: string;
    onUpload?: (blobPath: string, previewUrl: string) => void;
    onRemoveImage?: () => void;
};

export function ThumbnailCell({
    imageUrl,
    isEditing,
    previewUrl,
    slug,
    onUpload,
    onRemoveImage,
}: ThumbnailCellProps) {
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
            <img src={displayUrl} alt="" className="size-10 rounded object-cover" />
        ) : (
            <div
                className={cn(
                    'flex h-10 w-10 items-center justify-center rounded text-xl',
                    visual.iconBgClass,
                    visual.iconTextClass,
                )}
            >
                {visual.emoji}
            </div>
        );
    }

    return (
        <div className="group relative flex size-10 items-center justify-center overflow-hidden rounded bg-muted hover:bg-muted/80">
            {isUploading ? (
                <Loader2 size={16} className="animate-spin text-muted-foreground" />
            ) : displayUrl ? (
                <>
                    <img src={displayUrl} alt="" className="size-full object-cover" />
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
                    className={cn(
                        'flex h-10 w-10 cursor-pointer items-center justify-center rounded text-xl',
                        visual.iconBgClass,
                        visual.iconTextClass,
                    )}
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
