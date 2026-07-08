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
            <div className="relative mx-auto flex h-14 max-w-4xl items-center px-4 sm:px-6">
                <Link
                    to={APP_ROUTES.student.learnLesson(courseId, lessonId)}
                    className="inline-flex items-center gap-1.5 text-sm text-muted-foreground transition-colors hover:text-foreground"
                >
                    <ChevronLeft className="size-4" />
                    {t('header.backToLesson')}
                </Link>
                <div className="absolute left-1/2 flex -translate-x-1/2 items-center gap-2 text-sm font-medium">
                    <ClipboardList className="size-4 text-primary" />
                    {t('common:general.test')}
                </div>
            </div>
        </header>
    );
}
