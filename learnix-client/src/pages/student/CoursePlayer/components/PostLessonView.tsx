import { useTranslation } from 'react-i18next';
import { MarkdownRenderer } from '@/components/common/ui/MarkdownRenderer';
import { useLessonContent } from '@/hooks/lesson/useLessonContent';
import type { LessonProgressItemDto } from '@/types/progress.types';

interface PostLessonViewProps {
    lesson: LessonProgressItemDto;
    courseId: string;
}

export function PostLessonView({ lesson, courseId }: PostLessonViewProps) {
    const { t } = useTranslation('lessonPlayer');
    const { data, isLoading } = useLessonContent(courseId, lesson.lessonId);

    return (
        <div className="mx-auto max-w-3xl">
            <h1 className="mb-6 font-heading text-2xl font-bold">{lesson.title}</h1>

            {isLoading && (
                <div className="space-y-3">
                    {[100, 90, 95, 70, 85].map((w, i) => (
                        <div
                            key={i}
                            className="h-4 animate-pulse rounded bg-secondary"
                            style={{ width: `${w}%` }}
                        />
                    ))}
                </div>
            )}

            {!isLoading && data?.content && <MarkdownRenderer content={data.content} />}

            {!isLoading && !data?.content && (
                <p className="text-sm text-muted-foreground">{t('post.noContent')}</p>
            )}
        </div>
    );
}
