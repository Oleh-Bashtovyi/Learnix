import { useTranslation } from 'react-i18next';
import {
    CartesianGrid,
    Line,
    LineChart,
    ResponsiveContainer,
    Tooltip,
    XAxis,
    YAxis,
} from 'recharts';
import type { InstructorRatingTrendItem } from '@/types/instructorAnalytics.types';
import { useChartColors } from '../useChartColors';
import { ChartCard } from './ChartCard';
import { ChartTooltip } from './ChartTooltip';

interface RatingTrendChartProps {
    data?: InstructorRatingTrendItem[];
    isLoading?: boolean;
    isError?: boolean;
    onRetry?: () => void;
    className?: string;
}

export function RatingTrendChart({
    data,
    isLoading,
    isError,
    onRetry,
    className,
}: RatingTrendChartProps) {
    const { t } = useTranslation('instructorAnalytics');
    const colors = useChartColors();

    return (
        <ChartCard
            title={t('ratingTrend.title')}
            className={className}
            isLoading={isLoading}
            isError={isError}
            onRetry={onRetry}
            isEmpty={!data || data.length === 0}
        >
            <ResponsiveContainer width="100%" height={220}>
                <LineChart data={data} margin={{ top: 8, right: 12, bottom: 0, left: -8 }}>
                    <CartesianGrid vertical={false} stroke={colors.border} strokeDasharray="3 3" />
                    <XAxis
                        dataKey="month"
                        stroke={colors.muted}
                        fontSize={11}
                        tickLine={false}
                        axisLine={false}
                        minTickGap={20}
                    />
                    <YAxis
                        domain={[0, 5]}
                        ticks={[0, 1, 2, 3, 4, 5]}
                        width={28}
                        stroke={colors.muted}
                        fontSize={11}
                        tickLine={false}
                        axisLine={false}
                    />
                    <Tooltip
                        cursor={{ stroke: colors.border }}
                        content={({ active, payload, label }) => (
                            <ChartTooltip
                                active={active}
                                title={label as string}
                                rows={
                                    payload?.[0]
                                        ? [
                                              {
                                                  label: t('ratingTrend.average'),
                                                  value: Number(payload[0].value).toFixed(2),
                                                  color: colors.warning,
                                              },
                                              {
                                                  label: t('ratingTrend.reviews'),
                                                  value: payload[0].payload.reviewCount,
                                              },
                                          ]
                                        : []
                                }
                            />
                        )}
                    />
                    <Line
                        type="monotone"
                        dataKey="averageRating"
                        stroke={colors.warning}
                        strokeWidth={2}
                        dot={{ r: 3, fill: colors.warning, strokeWidth: 0 }}
                        activeDot={{ r: 5 }}
                    />
                </LineChart>
            </ResponsiveContainer>
        </ChartCard>
    );
}
