import { EnrollmentStatus, PaymentStatus } from '@/enums/enrollment.enums';

export interface EnrolledCourseDto {
    enrollmentId: string;
    courseId: string;
    courseTitle: string;
    courseCoverBlobPath: string | null;
    courseInstructorId: string;
    instructorName: string;
    courseCategoryId: string;
    pricePaid: number;
    enrollmentStatus: EnrollmentStatus;
    paymentStatus: PaymentStatus;
    enrolledAt: string;
    completedAt: string | null;
    coverImageUrl: string | null;
    completedLessons: number;
    totalLessons: number;
    myRating: number | null;
}

export interface EnrollRequest {
    courseId: string;
}

/** The course the student was last active in, and the lesson to resume at. */
export interface ContinueLearningDto {
    courseId: string;
    courseTitle: string;
    lessonId: string;
}
