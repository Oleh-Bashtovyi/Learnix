import { useQuery } from '@tanstack/react-query';
import { certificatesApi } from '@/api/certificates.api';
import { queryKeys } from '@/api/queryKeys';

export function useVerifyCertificate(code: string) {
    return useQuery({
        queryKey: queryKeys.certificates.verify(code),
        queryFn: () => certificatesApi.verifyCertificate(code),
        enabled: !!code,
        retry: false, // Don't retry on 404
    });
}
