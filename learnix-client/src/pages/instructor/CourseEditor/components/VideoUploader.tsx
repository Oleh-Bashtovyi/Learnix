import { useEffect, useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { RefreshCw, Video } from 'lucide-react';
import { useRequestUploadUrl } from '@/hooks/shared/useRequestUploadUrl';
import { cn } from '@/utils/cn';

interface Props {
    value: string;
    onChange: (blobPath: string, durationSeconds?: number) => void;
}

function extractVideoDuration(file: File): Promise<number> {
    return new Promise((resolve) => {
        const video = document.createElement('video');
        video.preload = 'metadata';
        video.onloadedmetadata = () => {
            URL.revokeObjectURL(video.src);
            resolve(video.duration);
        };
        video.onerror = () => {
            URL.revokeObjectURL(video.src);
            resolve(0); // fallback if it fails
        };
        video.src = URL.createObjectURL(file);
    });
}

export function VideoUploader({ value, onChange }: Props) {
    const { t } = useTranslation('instructor');
    const inputRef = useRef<HTMLInputElement>(null);
    const { uploadFile, isUploading, error } = useRequestUploadUrl();
    const [localPreview, setLocalPreview] = useState<string | null>(null);

    useEffect(() => {
        return () => {
            if (localPreview) {
                URL.revokeObjectURL(localPreview);
            }
        };
    }, [localPreview]);

    async function handleFile(file: File) {
        if (!file.type.startsWith('video/')) return;
        try {
            const duration = await extractVideoDuration(file);
            const blobPath = await uploadFile('LessonVideo', file);
            if (localPreview) URL.revokeObjectURL(localPreview);
            setLocalPreview(URL.createObjectURL(file));
            onChange(blobPath, Math.round(duration));
        } catch {
            // error surfaced via hook state
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

    const videoSrc = localPreview || (value?.startsWith('http') ? value : null);

    return (
        <div className="space-y-1.5">
            <label className="block text-sm font-medium text-foreground">{t('fieldVideo')}</label>
            {videoSrc && !isUploading ? (
                <div className="overflow-hidden rounded-lg border border-border bg-black">
                    <video src={videoSrc} controls className="aspect-video w-full object-contain" />
                    <div className="flex justify-end border-t border-border bg-card p-3">
                        <button
                            type="button"
                            onClick={() => inputRef.current?.click()}
                            className="flex items-center gap-2 rounded-md bg-secondary px-4 py-2 text-sm font-medium text-secondary-foreground transition-colors hover:bg-secondary/80"
                        >
                            <RefreshCw size={16} />
                            {t('replaceVideo')}
                        </button>
                    </div>
                </div>
            ) : (
                <div
                    onClick={() => !isUploading && inputRef.current?.click()}
                    onDrop={onDrop}
                    onDragOver={(e) => e.preventDefault()}
                    className={cn(
                        'flex cursor-pointer flex-col items-center gap-2 rounded-lg border-2 border-dashed border-border bg-muted p-8 text-muted-foreground transition-colors hover:border-primary',
                        isUploading && 'cursor-wait opacity-70',
                    )}
                >
                    <Video size={28} className={cn('opacity-50', isUploading && 'animate-pulse')} />
                    {isUploading ? (
                        <span className="text-sm">{t('videoUploading')}</span>
                    ) : (
                        <span className="text-sm">{t('videoHint')}</span>
                    )}
                </div>
            )}
            {error && <p className="text-xs text-destructive">{error}</p>}
            <input
                ref={inputRef}
                type="file"
                accept="video/mp4,video/webm"
                className="hidden"
                onChange={onInputChange}
            />
        </div>
    );
}
