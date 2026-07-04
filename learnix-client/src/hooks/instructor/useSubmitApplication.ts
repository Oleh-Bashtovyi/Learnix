import { useTranslation } from 'react-i18next';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import {
    type SubmitApplicationRequest,
    instructorApplicationsApi,
} from '@/api/instructorApplications.api';
import { queryKeys } from '@/api/queryKeys';
import { getErrorMessage } from '@/utils/errors';

export function useSubmitApplication() {
    const { t } = useTranslation('instructor');
    const qc = useQueryClient();
    return useMutation({
        mutationFn: (data: SubmitApplicationRequest) => instructorApplicationsApi.submit(data),
        onSuccess: () => {
            toast.success(t('toastApplicationSubmitted'));
            qc.invalidateQueries({ queryKey: queryKeys.applications.mine() });
        },
        onError: (error) => toast.error(getErrorMessage(error, t('toastError'))),
    });
}
