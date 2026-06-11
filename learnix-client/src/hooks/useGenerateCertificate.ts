import { useMutation, useQueryClient } from '@tanstack/react-query';
import { certificatesApi } from '@/api/certificates.api';
import { queryKeys } from '@/api/queryKeys';

export function useGenerateCertificate() {
    const queryClient = useQueryClient();

    return useMutation({
        mutationFn: async (courseId: string) => {
            const data = await certificatesApi.generateCourseCertificate(courseId);
            return data.url;
        },
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: queryKeys.certificates.mine() });
        },
    });
}
