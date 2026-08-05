import { useTranslation } from 'react-i18next';
import { Heart } from 'lucide-react';
import { EmptyState } from '@/components/common/elements/EmptyState';
import { QueryError } from '@/components/common/system/QueryError';
import { useWishlist } from '@/hooks/student/useWishlist';
import { APP_ROUTES } from '@/routes/paths';
import { WishlistCard } from './components/WishlistCard';

export default function WishlistPage() {
    const { t } = useTranslation('wishlist');
    const { data, isLoading, isError, refetch } = useWishlist();

    return (
        <div className="mx-auto max-w-7xl px-4 pb-12 pt-6 sm:px-6 sm:pb-16 sm:pt-8">
            {isLoading ? (
                <div className="grid gap-6 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
                    {[1, 2, 3, 4].map((i) => (
                        <div key={i} className="h-[300px] animate-pulse rounded-xl bg-card" />
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
                    icon={Heart}
                    title={t('emptyTitle')}
                    description={t('emptyDescription')}
                    action={{
                        to: APP_ROUTES.public.courses,
                        label: t('common:actions.browseCourses'),
                    }}
                />
            ) : (
                <div className="grid gap-6 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
                    {data?.items.map((course) => (
                        <WishlistCard key={course.courseId} course={course} />
                    ))}
                </div>
            )}
        </div>
    );
}
