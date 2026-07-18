import { useTranslation } from 'react-i18next';
import { Bar, BarChart, CartesianGrid, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts';
import type { RatingDistribution } from '@/types/instructorAnalytics.types';
import { useChartColors } from '../useChartColors';
import { ChartCard } from './ChartCard';
import { ChartTooltip } from './ChartTooltip';

interface RatingDistributionChartProps {
    distribution?: RatingDistribution;
    isLoading?: boolean;
    isError?: boolean;
    onRetry?: () => void;
    className?: string;
}

export function RatingDistributionChart({
    distribution,
    isLoading,
    isError,
    onRetry,
    className,
}: RatingDistributionChartProps) {
    const { t } = useTranslation('instructorAnalytics');
    const colors = useChartColors();

    const rows = [
        { star: 1, count: distribution?.oneStar ?? 0 },
        { star: 2, count: distribution?.twoStar ?? 0 },
        { star: 3, count: distribution?.threeStar ?? 0 },
        { star: 4, count: distribution?.fourStar ?? 0 },
        { star: 5, count: distribution?.fiveStar ?? 0 },
    ];

    const total = rows.reduce((sum, r) => sum + r.count, 0);

    return (
        <ChartCard
            title={t('ratings.title')}
            className={className}
            isLoading={isLoading}
            isError={isError}
            onRetry={onRetry}
            isEmpty={total === 0}
        >
            <ResponsiveContainer width="100%" height={200}>
                <BarChart data={rows} margin={{ top: 8, right: 8, bottom: 0, left: -16 }}>
                    <CartesianGrid vertical={false} stroke={colors.border} strokeDasharray="3 3" />
                    <XAxis
                        dataKey="star"
                        tickFormatter={(v: number) => t('ratings.starsLabel', { count: v })}
                        stroke={colors.muted}
                        fontSize={11}
                        tickLine={false}
                        axisLine={false}
                    />
                    <YAxis
                        allowDecimals={false}
                        stroke={colors.muted}
                        fontSize={11}
                        tickLine={false}
                        axisLine={false}
                    />
                    <Tooltip
                        cursor={{ fill: colors.border, fillOpacity: 0.3 }}
                        content={({ active, payload }) => (
                            <ChartTooltip
                                active={active}
                                title={
                                    payload?.[0]
                                        ? t('ratings.starsLabel', {
                                              count: payload[0].payload.star,
                                          })
                                        : undefined
                                }
                                rows={[
                                    {
                                        label: t('ratings.reviews'),
                                        value: payload?.[0]?.value ?? 0,
                                        color: colors.warning,
                                    },
                                ]}
                            />
                        )}
                    />
                    <Bar dataKey="count" fill={colors.warning} radius={[4, 4, 0, 0]} barSize={36} />
                </BarChart>
            </ResponsiveContainer>
        </ChartCard>
    );
}
