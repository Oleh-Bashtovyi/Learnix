import { CourseBadge, CourseStatus } from '@/enums/course.enums';
import { LessonType, QuestionType, TestReviewMode } from '@/enums/lesson.enums';

export interface LessonSummaryDto {
    id: string;
    title: string;
    order: number;
    lessonType: LessonType;
}

export interface SectionDetailDto {
    id: string;
    title: string;
    order: number;
    lessons: LessonSummaryDto[];
}

export interface CourseDetailDto {
    id: string;
    instructorId: string;
    categoryId: string;
    title: string;
    description: string;
    coverImageUrl: string | null;
    price: number;
    isFree: boolean;
    enrollmentsCount: number;
    averageRating: number;
    reviewsCount: number;
    tags: string[];
    sections: SectionDetailDto[];
    createdAt: string;
    updatedAt: string;
    instructorFullName: string;
}

export interface CategoryDto {
    id: string;
    name: string;
    slug: string;
    coursesCount: number;
}

/** Returned by GET /api/courses/mine */
export interface ManageCourseCardDto {
    id: string;
    instructorId: string;
    categoryId: string;
    title: string;
    description: string;
    coverImageUrl: string | null;
    price: number;
    isFree: boolean;
    status: CourseStatus;
    enrollmentsCount: number;
    tags: string[];
    createdAt: string;
    updatedAt: string;
    isDeleted: boolean;
}

/** Returned by GET /api/courses/{id}/edit */
export interface CourseForEditDto {
    id: string;
    instructorId: string;
    categoryId: string;
    title: string;
    description: string;
    coverImageUrl: string | null;
    price: number;
    isFree: boolean;
    status: CourseStatus;
    enrollmentsCount: number;
    tags: string[];
    sections: CourseForEditSectionDto[];
    createdAt: string;
    updatedAt: string;
}

export interface CourseForEditSectionDto {
    id: string;
    title: string;
    order: number;
    lessons: CourseForEditLessonDto[];
}

export interface CourseForEditLessonDto {
    id: string;
    title: string;
    order: number;
    lessonType: LessonType;
    isHidden: boolean;
    videoUrl: string | null;
    description: string | null;
    durationSeconds: number | null;
    content: string | null;
    attemptLimit: number | null;
    cooldownMinutes: number | null;
    passingThreshold: number | null;
    reviewMode: TestReviewMode | null;
    questions: CourseForEditQuestionDto[];
}

export interface CourseForEditQuestionDto {
    id: string;
    text: string;
    type: QuestionType;
    order: number;
    options: CourseForEditQuestionOptionDto[];
    correctAnswer: string | null;
    ignoreCase: boolean;
    allowFuzzy: boolean;
}

export interface CourseForEditQuestionOptionDto {
    id: string;
    text: string;
    isCorrect: boolean;
    order: number;
}

// Lightweight course representation

/**
 * Lightweight course representation for catalog/featured listings.
 */
export interface CourseSummaryDto {
    id: string;
    title: string;
    description: string;
    coverImageUrl: string | null;
    price: number; // 0 = free
    rating: number; // 0–5
    reviewsCount: number;
    durationHours: number;
    categoryName: string;
    instructor: {
        id: string;
        fullName: string;
    };
    badge?: CourseBadge | null;
}
