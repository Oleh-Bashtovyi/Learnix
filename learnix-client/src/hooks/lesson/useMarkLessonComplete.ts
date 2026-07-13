import { useTranslation } from 'react-i18next';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { progressApi } from '@/api/progress.api';
import { queryKeys } from '@/api/queryKeys';
import type { CourseProgressDto } from '@/types/progress.types';

export function useMarkLessonComplete(courseId: string) {
    const queryClient = useQueryClient();
    const { t } = useTranslation('lessonPlayer');

    return useMutation({
        mutationFn: (lessonId: string) => progressApi.markLessonComplete(courseId, lessonId),
        onMutate: async (lessonId) => {
            const queryKey = queryKeys.progress.course(courseId);
            await queryClient.cancelQueries({ queryKey });

            const previousProgress = queryClient.getQueryData<CourseProgressDto>(queryKey);

            if (previousProgress) {
                queryClient.setQueryData<CourseProgressDto>(queryKey, (old) => {
                    if (!old) return old;

                    // Prevent double-counting if optimistically clicked twice
                    const isAlreadyComplete = old.sections.some((s) =>
                        s.lessons.some((l) => l.lessonId === lessonId && l.isCompleted),
                    );

                    if (isAlreadyComplete) return old;

                    return {
                        ...old,
                        completedLessons: old.completedLessons + 1,
                        sections: old.sections.map((section) => ({
                            ...section,
                            lessons: section.lessons.map((lesson) =>
                                lesson.lessonId === lessonId
                                    ? {
                                          ...lesson,
                                          isCompleted: true,
                                          completedAt: new Date().toISOString(),
                                      }
                                    : lesson,
                            ),
                        })),
                    };
                });
            }

            return { previousProgress };
        },
        // No success toast: the optimistic update already ticked the lesson in the sidebar and moved
        // the progress bar before the request even returned, so a toast would only announce what the
        // user is looking at — while crowding out the one that matters, the unlocked achievement.
        // The failure toast stays: that one reports a rollback, which is not visible anywhere else.
        onError: (_err, _variables, context) => {
            if (context?.previousProgress) {
                queryClient.setQueryData(
                    queryKeys.progress.course(courseId),
                    context.previousProgress,
                );
            }
            toast.error(t('lesson.markCompleteFailed'));
        },
        onSettled: () => {
            queryClient.invalidateQueries({ queryKey: queryKeys.progress.course(courseId) });
        },
    });
}
