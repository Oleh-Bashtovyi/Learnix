import type { PaginatedResult } from '@/types/api.types';
import type { InstructorEarningsResponse, PaymentHistoryDto } from '@/types/payment.types';
import { api } from './axios.instance';

// The mock payment response (matches InitiateMockPaymentResponse)
export interface InitiateMockPaymentResponse {
    enrollmentId: string;
    courseId: string;
    status: string;
}

export const paymentsApi = {
    initiatePayment: (courseId: string) =>
        api.post<InitiateMockPaymentResponse>('/payments', { courseId }).then((r) => r.data),

    getMine: (skip: number = 0, take: number = 20) =>
        api
            .get<PaginatedResult<PaymentHistoryDto>>('/payments/mine', { params: { skip, take } })
            .then((r) => r.data),

    getInstructorEarnings: () =>
        api.get<InstructorEarningsResponse>('/instructor/earnings').then((r) => r.data),
};
