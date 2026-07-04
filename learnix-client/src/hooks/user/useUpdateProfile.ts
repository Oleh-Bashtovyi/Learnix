import { useMutation, useQueryClient } from '@tanstack/react-query';
import { queryKeys } from '@/api/queryKeys';
import { usersApi } from '@/api/users.api';
import { useAuthStore } from '@/store/auth.store';

export function useUpdateProfile() {
    const queryClient = useQueryClient();
    const setUser = useAuthStore((s) => s.setUser);
    const user = useAuthStore((s) => s.user);

    return useMutation({
        mutationFn: usersApi.updateProfile,
        onSuccess: async () => {
            await queryClient.invalidateQueries({ queryKey: queryKeys.users.myProfile() });
            queryClient.invalidateQueries({ queryKey: queryKeys.achievements.mine() });

            const updatedProfile = await queryClient.fetchQuery({
                queryKey: queryKeys.users.myProfile(),
                queryFn: usersApi.getMyProfile,
            });

            if (user) {
                setUser({
                    ...user,
                    fullName: `${updatedProfile.firstName} ${updatedProfile.lastName}`.trim(),
                    avatarUrl: updatedProfile.avatarUrl,
                });
            }
        },
    });
}
