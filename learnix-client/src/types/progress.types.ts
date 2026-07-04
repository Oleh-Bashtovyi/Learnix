export interface LessonProgressItemDto {
    lessonId: string;
    title: string;
    lessonType: string;
    displayOrder: number;
    isCompleted: boolean;
    completedAt: string | null;
    lastAccessedAt: string | null;
}

export interface SectionProgressDto {
    sectionId: string;
    title: string;
    displayOrder: number;
    lessons: LessonProgressItemDto[];
}

export interface CourseProgressDto {
    courseId: string;
    totalLessons: number;
    completedLessons: number;
    sections: SectionProgressDto[];
}

export interface MarkLessonCompleteResponse {
    lessonProgressId: string;
    isCompleted: boolean;
    completedAt: string | null;
}
