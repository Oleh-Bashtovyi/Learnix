import { EnrollmentStatus, PaymentStatus } from '@/enums/enrollment.enums';

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
