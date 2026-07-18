import { useTranslation } from 'react-i18next';
import { Cell, Pie, PieChart, ResponsiveContainer, Tooltip } from 'recharts';
import type { CourseStatuses } from '@/types/instructorAnalytics.types';
import { useChartColors } from '../useChartColors';
import { ChartCard } from './ChartCard';
import { ChartTooltip } from './ChartTooltip';

interface CourseStatusesChartProps {
    statuses?: CourseStatuses;
    isLoading?: boolean;
    isError?: boolean;
    onRetry?: () => void;
    className?: string;
}

export function CourseStatusesChart({
    statuses,
    isLoading,
    isError,
    onRetry,
    className,
}: CourseStatusesChartProps) {
    const { t } = useTranslation('instructorAnalytics');
    const colors = useChartColors();

    // Fixed, semantic order — the colour follows the status, matching the badges used elsewhere.
    const segments = [
        {
            key: 'published',
            label: t('statuses.published'),
            value: statuses?.published ?? 0,
            color: colors.success,
        },
        {
            key: 'draft',
            label: t('statuses.draft'),
            value: statuses?.draft ?? 0,
            color: colors.muted,
        },
        {
            key: 'archived',
            label: t('statuses.archived'),
            value: statuses?.archived ?? 0,
            color: colors.warning,
        },
    ];

    const total = segments.reduce((sum, s) => sum + s.value, 0);
    const visible = segments.filter((s) => s.value > 0);

    return (
        <ChartCard
            title={t('statuses.title')}
            className={className}
            isLoading={isLoading}
            isError={isError}
            onRetry={onRetry}
            isEmpty={total === 0}
        >
            <div className="flex flex-1 flex-col items-center justify-center gap-6 sm:flex-row sm:gap-10">
                <div className="relative">
                    <ResponsiveContainer width={180} height={180}>
                        <PieChart>
                            <Pie
                                data={visible}
                                dataKey="value"
                                nameKey="label"
                                innerRadius={58}
                                outerRadius={86}
                                paddingAngle={2}
                                stroke="none"
                            >
                                {visible.map((s) => (
                                    <Cell key={s.key} fill={s.color} />
                                ))}
                            </Pie>
                            <Tooltip
                                content={({ active, payload }) => (
                                    <ChartTooltip
                                        active={active}
                                        rows={
                                            payload?.[0]
                                                ? [
                                                      {
                                                          label: payload[0].payload.label,
                                                          value: payload[0].value ?? 0,
                                                          color: payload[0].payload.color,
                                                      },
                                                  ]
                                                : []
                                        }
                                    />
                                )}
                            />
                        </PieChart>
                    </ResponsiveContainer>
                    <div className="pointer-events-none absolute inset-0 flex flex-col items-center justify-center">
                        <span className="font-heading text-2xl font-bold text-foreground">
                            {total}
                        </span>
                        <span className="text-xs text-muted-foreground">
                            {t('statuses.totalCourses')}
                        </span>
                    </div>
                </div>

                <ul className="space-y-2">
                    {segments.map((s) => (
                        <li key={s.key} className="flex items-center gap-2 text-sm">
                            <span
                                className="size-2.5 shrink-0 rounded-full"
                                style={{ backgroundColor: s.color }}
                            />
                            <span className="text-muted-foreground">{s.label}</span>
                            <span className="ml-2 font-medium text-foreground">{s.value}</span>
                        </li>
                    ))}
                </ul>
            </div>
        </ChartCard>
    );
}
