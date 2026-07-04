import { api } from './axios.instance';

export interface SubmitApplicationRequest {
    motivationText: string;
    portfolioUrl?: string;
}

export interface MyApplicationResponse {
    id: string;
    status: 'Pending' | 'Approved' | 'Rejected';
    motivationText: string;
    portfolioUrl: string | null;
    rejectionReason: string | null;
    submittedAt: string;
    reviewedAt: string | null;
}

export const instructorApplicationsApi = {
    submit: (data: SubmitApplicationRequest) =>
        api.post<{ id: string }>('/instructor-applications', data).then((r) => r.data),

    getMine: () =>
        api.get<MyApplicationResponse>('/instructor-applications/mine').then((r) => r.data),
};
