import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { Heart } from 'lucide-react';
import { queryKeys } from '@/api/queryKeys';
import { wishlistApi } from '@/api/wishlist.api';
import { APP_ROUTES } from '@/routes/paths';
import { WishlistCard } from './components/WishlistCard';

export default function WishlistPage() {
    const { t } = useTranslation('wishlist');
    const { data, isLoading } = useQuery({
        queryKey: queryKeys.wishlist.mine(),
        queryFn: () => wishlistApi.getMine(0, 50),
    });

    return (
        <div className="mx-auto max-w-7xl px-6 pb-12 pt-6 sm:pb-16 sm:pt-8">
            {isLoading ? (
                <div className="grid gap-6 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
                    {[1, 2, 3, 4].map((i) => (
                        <div key={i} className="h-[300px] animate-pulse rounded-xl bg-card" />
                    ))}
                </div>
            ) : data?.items.length === 0 ? (
                <div className="mt-16 text-center">
                    <div className="mx-auto flex size-24 items-center justify-center rounded-full bg-accent/10">
                        <Heart className="size-12 text-accent" />
                    </div>
                    <h2 className="mt-6 font-heading text-2xl font-bold">{t('emptyTitle')}</h2>
                    <p className="mt-2 text-muted-foreground">{t('emptyDescription')}</p>
                    <Link
                        to={APP_ROUTES.public.courses}
                        className="mt-8 inline-flex h-11 items-center justify-center rounded-md bg-primary px-8 font-medium text-primary-foreground hover:bg-primary/90"
                    >
                        {t('browseCourses')}
                    </Link>
                </div>
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
