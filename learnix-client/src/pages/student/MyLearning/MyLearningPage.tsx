import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import { BookOpen } from 'lucide-react';
import { useMyEnrollments } from '@/hooks/student/useMyEnrollments';
import { APP_ROUTES } from '@/routes/paths';
import { EnrolledCourseCard } from './components/EnrolledCourseCard';

export default function MyLearningPage() {
    const { t } = useTranslation('myLearning');
    const { data, isLoading } = useMyEnrollments();

    return (
        <div className="mx-auto max-w-7xl px-4 pb-12 pt-6 sm:px-6 sm:pb-16 sm:pt-8">
            {isLoading ? (
                <div className="grid gap-4 sm:grid-cols-2 sm:gap-6 lg:grid-cols-3 xl:grid-cols-4">
                    {[1, 2, 3, 4].map((i) => (
                        <div key={i} className="h-[280px] animate-pulse rounded-xl bg-card" />
                    ))}
                </div>
            ) : data?.items.length === 0 ? (
                <div className="mt-16 text-center">
                    <div className="mx-auto flex size-24 items-center justify-center rounded-full bg-accent/10">
                        <BookOpen className="size-12 text-accent" />
                    </div>
                    <h2 className="mt-6 font-heading text-2xl font-bold">{t('emptyTitle')}</h2>
                    <p className="mt-2 text-muted-foreground">{t('emptyDescription')}</p>
                    <Link
                        to={APP_ROUTES.public.courses}
                        className="mt-6 inline-flex h-11 w-full items-center justify-center rounded-md bg-primary px-8 font-medium text-primary-foreground hover:bg-primary/90 sm:mt-8 sm:w-auto"
                    >
                        {t('browseCourses')}
                    </Link>
                </div>
            ) : (
                <div className="grid gap-4 sm:grid-cols-2 sm:gap-6 lg:grid-cols-3 xl:grid-cols-4">
                    {data?.items.map((enrollment) => (
                        <EnrolledCourseCard key={enrollment.enrollmentId} enrollment={enrollment} />
                    ))}
                </div>
            )}
        </div>
    );
}
