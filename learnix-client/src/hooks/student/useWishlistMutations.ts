import { useTranslation } from 'react-i18next';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { queryKeys } from '@/api/queryKeys';
import { wishlistApi } from '@/api/wishlist.api';

export function useAddToWishlist() {
    const { t } = useTranslation('wishlist');
    const queryClient = useQueryClient();
    return useMutation({
        mutationFn: (courseId: string) => wishlistApi.add(courseId),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: queryKeys.wishlist.mine() });
            toast.success(t('addedSuccess'));
        },
    });
}

export function useRemoveFromWishlist() {
    const { t } = useTranslation('wishlist');
    const queryClient = useQueryClient();
    return useMutation({
        mutationFn: (courseId: string) => wishlistApi.remove(courseId),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: queryKeys.wishlist.mine() });
            toast.success(t('removedSuccess'));
        },
    });
}
