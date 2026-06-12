import { api } from './axios.instance';
import type {
    MyCertificateDto,
    CourseCertificateResponse,
    VerifyCertificateResponse,
} from '@/types/certificate.types';

export const certificatesApi = {
    getMyCertificates: () => api.get<MyCertificateDto[]>('/certificates/mine').then((r) => r.data),

    getCourseCertificate: (courseId: string) =>
        api.get<CourseCertificateResponse>(`/certificates/courses/${courseId}`).then((r) => r.data),

    verifyCertificate: (code: string) =>
        api.get<VerifyCertificateResponse>(`/certificates/verify/${code}`).then((r) => r.data),

    generateCourseCertificate: (courseId: string) =>
        api.post<{ url: string }>(`/certificates/courses/${courseId}/generate`).then((r) => r.data),
};
