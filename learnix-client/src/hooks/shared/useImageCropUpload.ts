import { useCallback, useState } from 'react';
import { useTranslation } from 'react-i18next';
import {
    CROP_OUTPUT,
    IMAGE_CROP_RULES,
    type ImageUploadTarget,
    UPLOAD_CONTENT_TYPES,
    UPLOAD_MAX_BYTES,
    formatBytes,
} from '@/const/upload.constants';
import { useRequestUploadUrl } from '@/hooks/shared/useRequestUploadUrl';
import { type CropArea, cropImageToFile, readImageDimensions } from '@/utils/cropImage';

/**
 * Picker → validate → crop → blob, for every image upload on the platform.
 *
 * The caller owns the chrome (avatar circle, cover drop zone, category cell) and renders
 * <ImageCropperDialog {...cropper} />; this hook owns everything behind it.
 *
 * Nothing reaches Azure until the crop is confirmed, so a user who backs out of the dialog leaves
 * no temp blob behind at all.
 */
export function useImageCropUpload(
    target: ImageUploadTarget,
    onUploaded: (blobPath: string, previewUrl: string) => void,
) {
    const { t } = useTranslation('common');
    const { uploadFile, isUploading } = useRequestUploadUrl();
    const [imageUrl, setImageUrl] = useState<string | null>(null);
    const [preview, setPreview] = useState<string | null>(null);
    const [error, setError] = useState<string | null>(null);

    const rule = IMAGE_CROP_RULES[target];

    const close = useCallback(() => {
        setImageUrl((current) => {
            if (current) URL.revokeObjectURL(current);
            return null;
        });
    }, []);

    /** Validates the picked file and opens the cropper. Rejections surface as `error`. */
    async function selectFile(file: File) {
        setError(null);

        if (!UPLOAD_CONTENT_TYPES[target].includes(file.type)) {
            setError(t('upload.errors.contentType'));
            return;
        }

        const maxBytes = UPLOAD_MAX_BYTES[target];
        if (file.size > maxBytes) {
            setError(t('upload.errors.tooLarge', { max: formatBytes(maxBytes) }));
            return;
        }

        const url = URL.createObjectURL(file);
        try {
            const { width, height } = await readImageDimensions(url);
            if (width < rule.minSourceWidth || height < rule.minSourceHeight) {
                URL.revokeObjectURL(url);
                setError(
                    t('upload.errors.tooSmall', {
                        width: rule.minSourceWidth,
                        height: rule.minSourceHeight,
                    }),
                );
                return;
            }
        } catch {
            URL.revokeObjectURL(url);
            setError(t('upload.errors.unreadable'));
            return;
        }

        setImageUrl(url);
    }

    async function confirmCrop(area: CropArea) {
        if (!imageUrl) return;

        try {
            const file = await cropImageToFile(
                imageUrl,
                area,
                rule.outputWidth,
                rule.outputHeight,
                `${target}.${CROP_OUTPUT.contentType.split('/')[1]}`,
            );
            const blobPath = await uploadFile(target, file);

            // Preview the cropped result, not the original — that is what actually got stored.
            const previewUrl = URL.createObjectURL(file);
            setPreview((current) => {
                if (current) URL.revokeObjectURL(current);
                return previewUrl;
            });
            close();
            onUploaded(blobPath, previewUrl);
        } catch {
            setError(t('upload.errors.failed'));
        }
    }

    return {
        selectFile,
        isUploading,
        error,
        /** Object URL of the cropped image, for showing what was just uploaded. */
        preview,
        /** Spread straight into <ImageCropperDialog />. */
        cropper: {
            imageUrl,
            aspect: rule.aspect,
            isSaving: isUploading,
            onCancel: close,
            onSave: confirmCrop,
        },
    };
}
