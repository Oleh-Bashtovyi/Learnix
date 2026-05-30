export interface AdminUserDto {
    id: string;
    email: string;
    firstName: string;
    lastName: string;
    avatarUrl: string | null;
    roles: string[];
    isBanned: boolean;
    isDeleted: boolean;
    createdAt: string;
}

export interface PendingApplicationDto {
    id: string;
    userId: string;
    firstName: string;
    lastName: string;
    email: string;
    motivationText: string;
    portfolioUrl: string | null;
    submittedAt: string;
}

export interface MockPaymentDto {
    id: string;
    userName: string;
    userEmail: string;
    courseTitle: string;
    amount: number;
    status: 'Completed' | 'Pending' | 'Failed';
    createdAt: string;
}
