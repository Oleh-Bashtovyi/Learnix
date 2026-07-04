export interface CourseReviewDto {
    id: string;
    studentId: string;
    studentFirstName: string;
    studentLastName: string;
    studentAvatarBlobPath: string | null;
    rating: number;
    comment: string | null;
    createdAt: string;
    updatedAt: string;
}

export interface MyReviewDto {
    id: string;
    rating: number;
    comment: string | null;
    createdAt: string;
    updatedAt: string;
}

export interface CreateReviewRequest {
    rating: number;
    comment: string | null;
}

export interface UpdateReviewRequest {
    rating: number;
    comment: string | null;
}
