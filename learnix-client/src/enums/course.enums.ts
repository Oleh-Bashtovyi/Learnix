export const CourseStatus = {
    Draft: 'Draft',
    Published: 'Published',
    Archived: 'Archived',
} as const;
export type CourseStatus = (typeof CourseStatus)[keyof typeof CourseStatus];

export const CourseBadge = {
    Bestseller: 'bestseller',
    New: 'new',
} as const;
export type CourseBadge = (typeof CourseBadge)[keyof typeof CourseBadge];
