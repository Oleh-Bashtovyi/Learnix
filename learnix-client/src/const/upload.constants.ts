import type { UploadTarget } from '@/api/uploads.api';

/**
 * Single source of truth for what the client may upload.
 *
 * Content types and byte limits MIRROR THE BACKEND and must be kept in step with it:
 *   - allowed content types → RequestUploadUrlCommandValidator.AllowedContentTypes
 *   - byte limits           → AzureBlobStorageService.MaxSizes
 * The backend re-checks both on commit (and sniffs magic bytes), so these are a UX guard that
 * fails fast in the browser — never the security boundary.
 *
 * Pixel dimensions and aspect ratios exist only here: the backend does not decode images, so it
 * cannot enforce them. They shape the crop, not the trust model.
 */

const KB = 1024;
const MB = 1024 * KB;
const GB = 1024 * MB;

export const IMAGE_CONTENT_TYPES = ['image/jpeg', 'image/png', 'image/webp'] as const;
export const VIDEO_CONTENT_TYPES = ['video/mp4', 'video/webm'] as const;

export const UPLOAD_CONTENT_TYPES: Record<UploadTarget, readonly string[]> = {
    Avatar: IMAGE_CONTENT_TYPES,
    CourseCover: IMAGE_CONTENT_TYPES,
    CategoryImage: IMAGE_CONTENT_TYPES,
    LessonVideo: VIDEO_CONTENT_TYPES,
};

export const UPLOAD_MAX_BYTES: Record<UploadTarget, number> = {
    Avatar: 5 * MB,
    CourseCover: 10 * MB,
    CategoryImage: 2 * MB,
    LessonVideo: 2 * GB,
};

/** Targets that go through the cropper. LessonVideo is deliberately absent — video is not cropped. */
export type ImageUploadTarget = Extract<UploadTarget, 'Avatar' | 'CourseCover' | 'CategoryImage'>;

export const ASPECT_RATIOS = {
    /** Avatars and category tiles — rendered in circles and squares, so anything else gets cut off. */
    SQUARE: 1,
    /** Course covers and lesson video — the player and every card render 16:9. */
    WIDE: 16 / 9,
} as const;

interface ImageCropRule {
    /** Fixed crop aspect. The user can pan and zoom, but never change the shape. */
    aspect: number;
    /** Source images smaller than this are rejected — upscaling a crop of them would look mushy. */
    minSourceWidth: number;
    minSourceHeight: number;
    /** The cropped area is rendered to exactly this size, so stored images are uniform. */
    outputWidth: number;
    outputHeight: number;
}

export const IMAGE_CROP_RULES: Record<ImageUploadTarget, ImageCropRule> = {
    Avatar: {
        aspect: ASPECT_RATIOS.SQUARE,
        minSourceWidth: 100,
        minSourceHeight: 100,
        outputWidth: 512,
        outputHeight: 512,
    },
    CategoryImage: {
        aspect: ASPECT_RATIOS.SQUARE,
        minSourceWidth: 100,
        minSourceHeight: 100,
        outputWidth: 512,
        outputHeight: 512,
    },
    CourseCover: {
        aspect: ASPECT_RATIOS.WIDE,
        minSourceWidth: 640,
        minSourceHeight: 360,
        outputWidth: 1280,
        outputHeight: 720,
    },
};

/**
 * The cropper re-encodes to WebP: it is on the backend whitelist, keeps alpha (category tiles can
 * be transparent) and is markedly smaller than PNG at the same quality.
 */
export const CROP_OUTPUT = {
    contentType: 'image/webp',
    quality: 0.9,
    /** Zoom bounds for the crop dialog. */
    minZoom: 1,
    maxZoom: 3,
    zoomStep: 0.05,
} as const;

/**
 * Video cannot be cropped in the browser without re-encoding, so a non-16:9 file is a warning,
 * not a rejection — the player letterboxes it. Tolerance is on the ratio itself (16/9 ≈ 1.778),
 * so 4:3 (1.33) and 21:9 (2.33) both trip it while 1920x1088 does not.
 */
export const VIDEO_ASPECT = {
    ratio: ASPECT_RATIOS.WIDE,
    tolerance: 0.1,
} as const;

/** Value for an <input type="file"> accept attribute. */
export function acceptAttr(target: UploadTarget): string {
    return UPLOAD_CONTENT_TYPES[target].join(',');
}

export function formatBytes(bytes: number): string {
    if (bytes >= GB) return `${(bytes / GB).toFixed(1)} GB`;
    if (bytes >= MB) return `${Math.round(bytes / MB)} MB`;
    return `${Math.round(bytes / KB)} KB`;
}
