import { useEffect } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { PageFallback } from '@/components/common/system/PageFallback';
import { useCourseProgress } from '@/hooks/lesson/useCourseProgress';

export default function CourseStartPage() {
    const { courseId } = useParams<{ courseId: string }>();
    const navigate = useNavigate();
    const { data: progress } = useCourseProgress(courseId!);

    useEffect(() => {
        if (!progress) return;

        const firstLesson = progress.sections
            .slice()
            .sort((a, b) => a.displayOrder - b.displayOrder)
            .flatMap((s) => s.lessons.slice().sort((a, b) => a.displayOrder - b.displayOrder))
            .at(0);

        if (firstLesson) {
            navigate(`/courses/${courseId}/learn/${firstLesson.lessonId}`, { replace: true });
        }
    }, [progress, courseId, navigate]);

    return <PageFallback />;
}
