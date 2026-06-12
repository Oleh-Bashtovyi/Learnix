import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import { Heart } from 'lucide-react';
import { useTranslation } from 'react-i18next';

import { wishlistApi } from '@/api/wishlist.api';
import { queryKeys } from '@/api/queryKeys';
import { WishlistCard } from './components/WishlistCard';

export default function WishlistPage() {
    const { t } = useTranslation('wishlist');
    const { data, isLoading } = useQuery({
        queryKey: queryKeys.wishlist.mine(),
        queryFn: () => wishlistApi.getMine(0, 50),
    });

    return (
        <div className="mx-auto max-w-7xl px-6 py-10">
            {isLoading ? (
                <div className="mt-8 grid gap-6 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
                    {[1, 2, 3, 4].map((i) => (
                        <div key={i} className="h-[300px] animate-pulse rounded-xl bg-card" />
                    ))}
                </div>
            ) : data?.items.length === 0 ? (
                <div className="mt-16 text-center">
                    <div className="mx-auto flex h-24 w-24 items-center justify-center rounded-full bg-accent/10">
                        <Heart className="h-12 w-12 text-accent" />
                    </div>
                    <h2 className="mt-6 font-heading text-2xl font-bold">{t('emptyTitle')}</h2>
                    <p className="mt-2 text-muted-foreground">{t('emptyDescription')}</p>
                    <Link
                        to="/courses"
                        className="mt-8 inline-flex h-11 items-center justify-center rounded-md bg-primary px-8 font-medium text-primary-foreground hover:bg-primary/90"
                    >
                        {t('browseCourses')}
                    </Link>
                </div>
            ) : (
                <div className="mt-8 grid gap-6 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
                    {data?.items.map((course) => (
                        <WishlistCard key={course.courseId} course={course} />
                    ))}
                </div>
            )}
        </div>
    );
}
