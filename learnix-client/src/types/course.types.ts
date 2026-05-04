// learnix-client/src/types/course.types.ts

export interface CategoryDto {
    id: string;
    name: string;
    slug: string;
    coursesCount: number;
}

/**
 * Lightweight course representation for catalog/featured listings.
 * Full CourseDto will be added when course detail page is implemented.
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
    badge?: 'bestseller' | 'new' | null;
}
