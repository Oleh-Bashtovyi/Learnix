import { useQuery } from '@tanstack/react-query';
import { queryKeys } from '@/api/queryKeys';
import { wishlistApi } from '@/api/wishlist.api';
import { useAuthStore } from '@/store/auth.store';

export function useWishlist() {
    const user = useAuthStore((s) => s.user);

    const query = useQuery({
        queryKey: queryKeys.wishlist.mine(),
        queryFn: () => wishlistApi.getMine(0, 200),
        enabled: !!user,
        staleTime: 1000 * 60 * 2,
    });

    const isInWishlist = (courseId: string): boolean =>
        query.data?.items.some((item) => item.courseId === courseId) ?? false;

    return { ...query, isInWishlist };
}
