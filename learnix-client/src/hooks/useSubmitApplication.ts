import { useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { useTranslation } from 'react-i18next';
import {
    instructorApplicationsApi,
    type SubmitApplicationRequest,
} from '@/api/instructorApplications.api';
import { queryKeys } from '@/api/queryKeys';

export function useSubmitApplication() {
    const { t } = useTranslation('instructor');
    const qc = useQueryClient();
    return useMutation({
        mutationFn: (data: SubmitApplicationRequest) => instructorApplicationsApi.submit(data),
        onSuccess: () => {
            toast.success(t('toastApplicationSubmitted'));
            qc.invalidateQueries({ queryKey: queryKeys.applications.mine() });
        },
        onError: () => toast.error(t('toastError')),
    });
}
