export type EnrollmentStatus = 'Active' | 'Completed' | 'Suspended';
export type PaymentStatus = 'Free' | 'Pending' | 'Completed' | 'Failed' | 'Refunded';

export interface EnrolledCourseDto {
    enrollmentId: string;
    courseId: string;
    courseTitle: string;
    courseCoverBlobPath: string | null;
    courseInstructorId: string;
    courseCategoryId: string;
    pricePaid: number;
    enrollmentStatus: EnrollmentStatus;
    paymentStatus: PaymentStatus;
    enrolledAt: string;
    completedAt: string | null;
    coverImageUrl: string | null;
}

export interface EnrollRequest {
    courseId: string;
}
