import { useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { wishlistApi } from '@/api/wishlist.api';
import { queryKeys } from '@/api/queryKeys';
import { WISHLIST_PAGE } from '@/const/localization/wishlistPage';

export function useAddToWishlist() {
    const queryClient = useQueryClient();
    return useMutation({
        mutationFn: (courseId: string) => wishlistApi.add(courseId),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: queryKeys.wishlist.mine() });
            toast.success(WISHLIST_PAGE.addedSuccess);
        },
    });
}

export function useRemoveFromWishlist() {
    const queryClient = useQueryClient();
    return useMutation({
        mutationFn: (courseId: string) => wishlistApi.remove(courseId),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: queryKeys.wishlist.mine() });
            toast.success(WISHLIST_PAGE.removedSuccess);
        },
    });
}
