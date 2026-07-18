import { useTranslation } from 'react-i18next';
import { BookOpen } from 'lucide-react';
import { EmptyState } from '@/components/common/elements/EmptyState';
import { QueryError } from '@/components/common/system/QueryError';
import { useMyEnrollments } from '@/hooks/student/useMyEnrollments';
import { APP_ROUTES } from '@/routes/paths';
import { EnrolledCourseCard } from './components/EnrolledCourseCard';

export default function MyLearningPage() {
    const { t } = useTranslation('myLearning');
    const { data, isLoading, isError, refetch } = useMyEnrollments();

    return (
        <div className="mx-auto max-w-7xl px-4 pb-12 pt-6 sm:px-6 sm:pb-16 sm:pt-8">
            {isLoading ? (
                <div className="grid gap-4 sm:grid-cols-2 sm:gap-6 lg:grid-cols-3 xl:grid-cols-4">
                    {[1, 2, 3, 4].map((i) => (
                        <div key={i} className="h-[280px] animate-pulse rounded-xl bg-card" />
                    ))}
                </div>
            ) : isError ? (
                <QueryError
                    message={t('error.title')}
                    onRetry={refetch}
                    retryLabel={t('common:actions.tryAgain')}
                />
            ) : !data?.items.length ? (
                <EmptyState
                    icon={BookOpen}
                    title={t('emptyTitle')}
                    description={t('emptyDescription')}
                    action={{
                        to: APP_ROUTES.public.courses,
                        label: t('common:actions.browseCourses'),
                    }}
                />
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
