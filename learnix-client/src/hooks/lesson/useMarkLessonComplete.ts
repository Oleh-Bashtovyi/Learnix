import { useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { progressApi } from '@/api/progress.api';
import { queryKeys } from '@/api/queryKeys';
import type { CourseProgressDto } from '@/types/progress.types';

export function useMarkLessonComplete(courseId: string) {
    const queryClient = useQueryClient();

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
        onError: (_err, _variables, context) => {
            if (context?.previousProgress) {
                queryClient.setQueryData(
                    queryKeys.progress.course(courseId),
                    context.previousProgress,
                );
            }
            toast.error('Failed to mark lesson as complete');
        },
        onSuccess: () => {
            toast.success('Lesson marked as complete');
        },
        onSettled: () => {
            queryClient.invalidateQueries({ queryKey: queryKeys.progress.course(courseId) });
        },
    });
}
