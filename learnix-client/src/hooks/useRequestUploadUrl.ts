import { useState } from 'react';
import { type UploadTarget, uploadsApi } from '@/api/uploads.api';

export interface UploadState {
    isUploading: boolean;
    error: string | null;
}

export function useRequestUploadUrl() {
    const [state, setState] = useState<UploadState>({ isUploading: false, error: null });

    async function uploadFile(target: UploadTarget, file: File): Promise<string> {
        setState({ isUploading: true, error: null });
        try {
            const { uploadUrl, blobPath } = await uploadsApi.requestUploadUrl(target, file.type);
            await uploadsApi.uploadToBlob(uploadUrl, file);
            setState({ isUploading: false, error: null });
            return blobPath;
        } catch {
            const msg = 'Upload failed. Please try again.';
            setState({ isUploading: false, error: msg });
            throw new Error(msg);
        }
    }

    return { uploadFile, ...state };
}
