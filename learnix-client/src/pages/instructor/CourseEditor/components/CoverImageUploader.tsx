import { useRef } from 'react';
import { useTranslation } from 'react-i18next';
import { ImagePlus } from 'lucide-react';
import { ImageCropperDialog } from '@/components/common/upload/ImageCropperDialog';
import { acceptAttr } from '@/const/upload.constants';
import { useImageCropUpload } from '@/hooks/shared/useImageCropUpload';
import { cn } from '@/utils/cn';

interface Props {
    value: string | null;
    onChange: (blobPath: string) => void;
}

export function CoverImageUploader({ value, onChange }: Props) {
    const { t } = useTranslation('instructor');
    const inputRef = useRef<HTMLInputElement>(null);
    const { selectFile, isUploading, error, preview, cropper } = useImageCropUpload(
        'CourseCover',
        onChange,
    );

    function onInputChange(e: React.ChangeEvent<HTMLInputElement>) {
        const file = e.target.files?.[0];
        // Reset the input so picking the same file twice still fires a change event.
        e.target.value = '';
        if (file) selectFile(file);
    }

    function onDrop(e: React.DragEvent) {
        e.preventDefault();
        const file = e.dataTransfer.files?.[0];
        if (file) selectFile(file);
    }

    const displayUrl = preview ?? value;

    return (
        <div className="space-y-1.5">
            <label className="block text-sm font-medium text-foreground">
                {t('coverImageLabel')}
            </label>
            {/* A button, not a div: the drop zone opens a file picker, so it has to be reachable and
                activatable from the keyboard like any other control. */}
            <button
                type="button"
                disabled={isUploading}
                onClick={() => inputRef.current?.click()}
                onDrop={onDrop}
                onDragOver={(e) => e.preventDefault()}
                className={cn(
                    'relative aspect-video w-full cursor-pointer overflow-hidden rounded-lg border-2 border-dashed border-border bg-muted transition-colors hover:border-primary',
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
                                ? t('common:actions.uploading')
                                : value
                                  ? t('coverImageReplace')
                                  : t('coverImageHint')}
                        </span>
                    </div>
                )}
                {isUploading && (
                    <div className="absolute inset-0 flex items-center justify-center bg-background/60">
                        <span className="text-sm text-muted-foreground">
                            {t('common:actions.uploading')}
                        </span>
                    </div>
                )}
            </button>
            {error && <p className="text-xs text-destructive">{error}</p>}
            <input
                ref={inputRef}
                type="file"
                accept={acceptAttr('CourseCover')}
                className="hidden"
                onChange={onInputChange}
            />

            <ImageCropperDialog {...cropper} />
        </div>
    );
}
