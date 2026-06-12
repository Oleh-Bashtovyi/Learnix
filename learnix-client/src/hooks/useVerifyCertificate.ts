import { useQuery } from '@tanstack/react-query';
import { certificatesApi } from '@/api/certificates.api';

export function useVerifyCertificate(code: string) {
    return useQuery({
        queryKey: ['certificate', 'verify', code],
        queryFn: () => certificatesApi.verifyCertificate(code),
        enabled: !!code,
        retry: false, // Don't retry on 404
    });
}
