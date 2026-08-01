import { useTranslation } from 'react-i18next';
import { DollarSign, ShoppingCart } from 'lucide-react';
import { StatTile } from '@/components/common/elements/StatTile';
import { StatValueSkeleton } from '@/components/common/elements/StatValueSkeleton';
import { useInstructorEarningsQuery } from '@/hooks/instructor/useInstructorEarningsQuery';
import { EarningsTable } from '../components/EarningsTable';

export function EarningsTab() {
    const { t } = useTranslation('instructorAnalytics');
    const { data, isLoading, isError, refetch } = useInstructorEarningsQuery();

    const totalEarnings = (data?.totalEarnings ?? 0).toLocaleString(undefined, {
        style: 'currency',
        currency: 'USD',
    });

    return (
        <div className="space-y-6">
            <dl className="grid gap-4 sm:grid-cols-2">
                <StatTile
                    icon={<DollarSign className="size-5" />}
                    tone="brand"
                    surface="card"
                    label={t('earnings.total')}
                    value={isLoading ? <StatValueSkeleton /> : totalEarnings}
                />
                <StatTile
                    icon={<ShoppingCart className="size-5" />}
                    tone="accent"
                    surface="card"
                    label={t('earnings.payments')}
                    value={
                        isLoading ? (
                            <StatValueSkeleton />
                        ) : (
                            (data?.totalPayments ?? 0).toLocaleString()
                        )
                    }
                />
            </dl>

            <EarningsTable
                data={data?.courses}
                total={data?.totalEarnings}
                isLoading={isLoading}
                isError={isError}
                onRetry={() => refetch()}
                action={
                    <span className="rounded-full border border-warning/30 bg-warning/10 px-3 py-1 text-xs font-medium text-warning">
                        {t('earnings.freeBadge')}
                    </span>
                }
            />
        </div>
    );
}
