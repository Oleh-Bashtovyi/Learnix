import { PaymentStatus } from '@/enums/enrollment.enums';

export interface CourseEarningsDto {
    courseId: string;
    courseTitle: string;
    paymentsCount: number;
    totalAmount: number;
    lastPaymentAt: string;
}

export interface InstructorEarningsResponse {
    totalEarnings: number;
    totalPayments: number;
    courses: CourseEarningsDto[];
}

export interface PaymentHistoryDto {
    id: string;
    courseId: string;
    courseTitle: string;
    amount: number;
    currency: string;
    status: PaymentStatus;
    paymentProvider: string;
    createdAt: string;
    completedAt: string | null;
}
