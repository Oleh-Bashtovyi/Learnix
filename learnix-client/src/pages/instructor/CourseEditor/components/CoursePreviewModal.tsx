import { useTranslation } from 'react-i18next';
import { Clock, Star, Tag, Users } from 'lucide-react';
import { toast } from 'sonner';
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { useCategories } from '@/hooks/course/useCategories';
import { CourseSidebar } from '@/pages/public/CourseDetail/components/CourseSidebar';
import { CurriculumAccordion } from '@/pages/public/CourseDetail/components/CurriculumAccordion';
import { ReviewsList } from '@/pages/public/CourseDetail/components/ReviewsList';
import { useAuthStore } from '@/store/auth.store';
import type { CourseForEditDto, SectionDetailDto } from '@/types/course.types';

interface CoursePreviewModalProps {
    course: CourseForEditDto;
    onClose: () => void;
}

/**
 * A draft/unpublished course 404s on GET /courses/{id} (only Published courses resolve there), so
 * this can't fetch the real CourseDetailDto — it reuses the same presentational subcomponents
 * (CourseSidebar, CurriculumAccordion, ReviewsList) fed from the already-loaded edit data instead.
 * Rating and review count are always 0: a course still being edited has no real ones to show yet.
 */
export function CoursePreviewModal({ course, onClose }: CoursePreviewModalProps) {
    const { t } = useTranslation(['instructor', 'courseDetail']);
    const user = useAuthStore((s) => s.user);
    const { data: categories = [] } = useCategories();
    const categoryName = categories.find((c) => c.id === course.categoryId)?.name ?? '';

    // A section with nothing left to show — empty, or holding only hidden lessons — is not part of
    // what a student sees, and this dialog shows exactly what they will see.
    const sections: SectionDetailDto[] = course.sections
        .map((section) => ({
            id: section.id,
            title: section.title,
            order: section.order,
            lessons: section.lessons
                .filter((lesson) => !lesson.isHidden)
                .map((lesson) => ({
                    id: lesson.id,
                    title: lesson.title,
                    order: lesson.order,
                    lessonType: lesson.lessonType,
                    durationSeconds: lesson.durationSeconds,
                    readingSeconds: lesson.readingSeconds,
                    questionsCount: lesson.questions.length || null,
                })),
        }))
        .filter((section) => section.lessons.length > 0);

    const totalLessons = sections.reduce((sum, s) => sum + s.lessons.length, 0);
    const isFree = course.price === 0;

    function notAvailableInPreview() {
        toast.info(t('instructor:preview.actionDisabled'));
    }

    // Shaped like CourseDetailDto so CourseSidebar can be reused as-is.
    const previewCourse = {
        id: course.id,
        instructorId: course.instructorId,
        categoryId: course.categoryId,
        categoryName,
        title: course.title,
        description: course.description,
        coverImageUrl: course.coverImageUrl,
        price: course.price,
        isFree,
        enrollmentsCount: course.enrollmentsCount,
        averageRating: 0,
        reviewsCount: 0,
        tags: course.tags,
        sections,
        createdAt: course.createdAt,
        updatedAt: course.updatedAt,
        instructorFullName: user?.fullName ?? '',
    };

    return (
        <Dialog open onOpenChange={(open) => !open && onClose()}>
            <DialogContent className="flex max-h-[90vh] max-w-5xl flex-col overflow-y-auto">
                <DialogHeader>
                    <DialogTitle>{t('instructor:preview.title')}</DialogTitle>
                </DialogHeader>

                <div className="flex flex-col gap-8 lg:grid lg:grid-cols-[1fr_320px]">
                    <div className="min-w-0 space-y-8">
                        <div>
                            {previewCourse.categoryName && (
                                <span className="inline-block rounded-md bg-accent/10 px-2.5 py-1 text-xs font-semibold text-accent-strong">
                                    {previewCourse.categoryName}
                                </span>
                            )}
                            <h1 className="mt-2 font-heading text-3xl font-bold text-foreground">
                                {previewCourse.title}
                            </h1>

                            <div className="mt-3 flex flex-wrap items-center gap-4 text-sm text-muted-foreground">
                                <div className="flex items-center gap-1">
                                    <Star className="size-4 fill-warning text-warning" />
                                    <span className="font-medium text-foreground">—</span>
                                </div>
                                <div className="flex items-center gap-1">
                                    <Users className="size-4" />
                                    <span>
                                        {t('instructor:preview.meta.students', {
                                            count: previewCourse.enrollmentsCount,
                                        })}
                                    </span>
                                </div>
                                <div className="flex items-center gap-1">
                                    <Clock className="size-4" />
                                    <span>
                                        {t('instructor:preview.meta.lessons', {
                                            count: totalLessons,
                                        })}
                                    </span>
                                </div>
                            </div>

                            {previewCourse.tags.length > 0 && (
                                <div className="mt-3 flex flex-wrap gap-2">
                                    {previewCourse.tags.map((tag) => (
                                        <span
                                            key={tag}
                                            className="inline-flex items-center gap-1 rounded-full bg-muted px-2.5 py-0.5 text-xs text-muted-foreground"
                                        >
                                            <Tag className="size-3" />
                                            {tag}
                                        </span>
                                    ))}
                                </div>
                            )}

                            <p className="mt-4 whitespace-pre-wrap text-muted-foreground">
                                {previewCourse.description}
                            </p>
                        </div>

                        {sections.length > 0 ? (
                            <CurriculumAccordion sections={sections} />
                        ) : (
                            <p className="text-sm text-muted-foreground">
                                {t('courseDetail:curriculum.empty')}
                            </p>
                        )}

                        <ReviewsList
                            reviews={[]}
                            averageRating={0}
                            totalCount={0}
                            composer={
                                <p className="rounded-xl border border-dashed border-border p-4 text-center text-sm text-muted-foreground">
                                    {t('courseDetail:reviews.enrollToReview')}
                                </p>
                            }
                        />
                    </div>

                    <CourseSidebar
                        course={previewCourse}
                        isFree={isFree}
                        isOwnCourse={false}
                        isEnrolled={false}
                        user={user}
                        inWishlist={false}
                        enrollIsPending={false}
                        onEnroll={notAvailableInPreview}
                        onToggleWishlist={notAvailableInPreview}
                        wishlistIsPending={false}
                    />
                </div>
            </DialogContent>
        </Dialog>
    );
}
