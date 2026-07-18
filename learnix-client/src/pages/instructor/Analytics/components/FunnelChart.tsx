import { useTranslation } from 'react-i18next';
import type { InstructorEngagement } from '@/types/instructorAnalytics.types';
import { ChartCard } from './ChartCard';

interface FunnelChartProps {
    data?: InstructorEngagement;
    isLoading?: boolean;
    isError?: boolean;
    onRetry?: () => void;
    className?: string;
}

export function FunnelChart({ data, isLoading, isError, onRetry, className }: FunnelChartProps) {
    const { t } = useTranslation('instructorAnalytics');

    const enrolled = data?.enrolled ?? 0;
    const stages = [
        { key: 'enrolled', label: t('funnel.enrolled'), value: data?.enrolled ?? 0 },
        { key: 'started', label: t('funnel.started'), value: data?.started ?? 0 },
        { key: 'completed', label: t('funnel.completed'), value: data?.completed ?? 0 },
        { key: 'certified', label: t('funnel.certified'), value: data?.certified ?? 0 },
    ];

    return (
        <ChartCard
            title={t('funnel.title')}
            className={className}
            isLoading={isLoading}
            isError={isError}
            onRetry={onRetry}
            isEmpty={enrolled === 0}
        >
            <div className="space-y-4">
                {stages.map((stage) => {
                    const pct = enrolled > 0 ? Math.round((stage.value / enrolled) * 100) : 0;
                    return (
                        <div key={stage.key}>
                            <div className="mb-1 flex items-baseline justify-between text-sm">
                                <span className="font-medium text-foreground">{stage.label}</span>
                                <span className="text-muted-foreground">
                                    <span className="font-medium text-foreground">
                                        {stage.value}
                                    </span>{' '}
                                    · {pct}%
                                </span>
                            </div>
                            <div className="h-2.5 w-full overflow-hidden rounded-full bg-muted">
                                <div
                                    className="h-full rounded-full bg-accent transition-all"
                                    style={{ width: `${pct}%` }}
                                />
                            </div>
                        </div>
                    );
                })}
            </div>
        </ChartCard>
    );
}
