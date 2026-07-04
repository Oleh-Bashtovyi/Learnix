import { useQuery } from '@tanstack/react-query';
import { certificatesApi } from '@/api/certificates.api';
import { queryKeys } from '@/api/queryKeys';

export function useMyCertificates() {
    return useQuery({
        queryKey: queryKeys.certificates.mine(),
        queryFn: certificatesApi.getMyCertificates,
    });
}
