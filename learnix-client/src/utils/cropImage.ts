import { CROP_OUTPUT } from '@/const/upload.constants';

/** Pixel rectangle in the SOURCE image's coordinate space, as react-easy-crop reports it. */
export interface CropArea {
    x: number;
    y: number;
    width: number;
    height: number;
}

export interface ImageDimensions {
    width: number;
    height: number;
}

export function loadImage(src: string): Promise<HTMLImageElement> {
    return new Promise((resolve, reject) => {
        const image = new Image();
        image.addEventListener('load', () => resolve(image));
        image.addEventListener('error', () => reject(new Error('Could not read the image.')));
        image.src = src;
    });
}

export async function readImageDimensions(objectUrl: string): Promise<ImageDimensions> {
    const image = await loadImage(objectUrl);
    return { width: image.naturalWidth, height: image.naturalHeight };
}

/** Reads a video's intrinsic size without playing it — used to warn about non-16:9 uploads. */
export function readVideoDimensions(objectUrl: string): Promise<ImageDimensions> {
    return new Promise((resolve, reject) => {
        const video = document.createElement('video');
        video.preload = 'metadata';
        video.addEventListener('loadedmetadata', () =>
            resolve({ width: video.videoWidth, height: video.videoHeight }),
        );
        video.addEventListener('error', () => reject(new Error('Could not read the video.')));
        video.src = objectUrl;
    });
}

/**
 * Draws the selected area onto a canvas sized to the target's output dimensions, so every stored
 * image of a given kind has identical pixel dimensions regardless of what the user uploaded.
 */
export async function cropImageToFile(
    objectUrl: string,
    area: CropArea,
    outputWidth: number,
    outputHeight: number,
    fileName: string,
): Promise<File> {
    const image = await loadImage(objectUrl);

    const canvas = document.createElement('canvas');
    canvas.width = outputWidth;
    canvas.height = outputHeight;

    const ctx = canvas.getContext('2d');
    if (!ctx) throw new Error('Could not create a drawing context.');

    ctx.imageSmoothingQuality = 'high';
    ctx.drawImage(image, area.x, area.y, area.width, area.height, 0, 0, outputWidth, outputHeight);

    const blob = await new Promise<Blob | null>((resolve) =>
        canvas.toBlob(resolve, CROP_OUTPUT.contentType, CROP_OUTPUT.quality),
    );
    if (!blob) throw new Error('Could not encode the cropped image.');

    return new File([blob], fileName, { type: CROP_OUTPUT.contentType });
}
