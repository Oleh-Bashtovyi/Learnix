import axios from 'axios';
import { api } from './axios.instance';

export type UploadTarget = 'Avatar' | 'CourseCover' | 'LessonVideo' | 'CategoryImage';

export interface UploadUrlResponse {
    uploadUrl: string;
    blobPath: string;
    expiresAt: string;
}

export const uploadsApi = {
    requestUploadUrl: (target: UploadTarget, contentType: string) =>
        api
            .post<UploadUrlResponse>('/uploads/request-url', { target, contentType })
            .then((r) => r.data),

    /** Upload file directly to Azure Blob via SAS URL — no auth header needed. */
    uploadToBlob: (uploadUrl: string, file: File) =>
        axios.put(uploadUrl, file, {
            headers: { 'x-ms-blob-type': 'BlockBlob', 'Content-Type': file.type },
        }),
};
