export const EnrollmentStatus = {
    Active: 'Active',
    Completed: 'Completed',
    Suspended: 'Suspended',
} as const;
export type EnrollmentStatus = (typeof EnrollmentStatus)[keyof typeof EnrollmentStatus];

export const PaymentStatus = {
    Free: 'Free',
    Pending: 'Pending',
    Completed: 'Completed',
    Failed: 'Failed',
    Refunded: 'Refunded',
} as const;
export type PaymentStatus = (typeof PaymentStatus)[keyof typeof PaymentStatus];
