import { useTranslation } from 'react-i18next';
import { Award, DollarSign, Star, Users } from 'lucide-react';
import { StatTile } from '@/components/common/elements/StatTile';
import type { InstructorAnalyticsSummary } from '@/types/instructorAnalytics.types';

interface StatCardsProps {
    summary?: InstructorAnalyticsSummary;
    isLoading?: boolean;
}

function Skeleton() {
    return <span className="inline-block h-5 w-14 animate-pulse rounded bg-muted" />;
}

export function StatCards({ summary, isLoading }: StatCardsProps) {
    const { t } = useTranslation('instructorAnalytics');

    const revenue = (summary?.totalRevenue ?? 0).toLocaleString(undefined, {
        style: 'currency',
        currency: 'USD',
        maximumFractionDigits: 0,
    });

    return (
        <dl className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
            <StatTile
                icon={<Users className="size-5" />}
                tone="accent"
                surface="card"
                label={t('stats.students')}
                value={isLoading ? <Skeleton /> : (summary?.totalStudents ?? 0).toLocaleString()}
            />
            <StatTile
                icon={<DollarSign className="size-5" />}
                tone="brand"
                surface="card"
                label={t('stats.revenue')}
                value={isLoading ? <Skeleton /> : revenue}
            />
            <StatTile
                icon={<Star className="size-5" />}
                tone="warning"
                surface="card"
                label={t('stats.averageRating')}
                value={isLoading ? <Skeleton /> : (summary?.averageRating ?? 0).toFixed(2)}
            />
            <StatTile
                icon={<Award className="size-5" />}
                tone="success"
                surface="card"
                label={t('stats.certificates')}
                value={
                    isLoading ? <Skeleton /> : (summary?.certificatesIssued ?? 0).toLocaleString()
                }
            />
        </dl>
    );
}
