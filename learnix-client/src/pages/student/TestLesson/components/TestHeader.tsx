import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import { ChevronLeft, ClipboardList } from 'lucide-react';
import { APP_ROUTES } from '@/routes/paths';

interface TestHeaderProps {
    courseId: string;
    lessonId: string;
}

export function TestHeader({ courseId, lessonId }: TestHeaderProps) {
    const { t } = useTranslation('testLesson');

    return (
        <header className="sticky top-0 z-10 border-b border-border bg-card">
            <div className="mx-auto flex h-14 max-w-4xl items-center justify-between px-6">
                <Link
                    to={APP_ROUTES.student.learnLesson(courseId, lessonId)}
                    className="inline-flex items-center gap-1.5 text-sm text-muted-foreground transition-colors hover:text-foreground"
                >
                    <ChevronLeft className="size-4" />
                    {t('header.backToLesson')}
                </Link>
                <div className="flex items-center gap-2 text-sm font-medium">
                    <ClipboardList className="size-4 text-primary" />
                    {t('header.testLabel')}
                </div>
                <div className="w-24" />
            </div>
        </header>
    );
}
