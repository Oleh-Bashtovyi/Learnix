import { useTranslation } from 'react-i18next';
import {
    Bar,
    BarChart,
    CartesianGrid,
    ResponsiveContainer,
    Tooltip,
    type TooltipContentProps,
    XAxis,
    YAxis,
} from 'recharts';
import type { CoursePopularityItem } from '@/types/instructorAnalytics.types';
import { useChartColors } from '../useChartColors';
import { ChartCard } from './ChartCard';
import { ChartTooltip } from './ChartTooltip';

interface PopularityChartProps {
    data?: CoursePopularityItem[];
    isLoading?: boolean;
    isError?: boolean;
    onRetry?: () => void;
    className?: string;
}

const MAX_BARS = 8;

interface PopularityTooltipContentProps extends Partial<TooltipContentProps<number, string>> {
    enrollmentsLabel: string;
    color: string;
}

function PopularityTooltipContent({
    active,
    payload,
    enrollmentsLabel,
    color,
}: PopularityTooltipContentProps) {
    return (
        <ChartTooltip
            active={active}
            title={payload?.[0]?.payload?.title}
            rows={[
                {
                    label: enrollmentsLabel,
                    value: payload?.[0]?.value ?? 0,
                    color,
                },
            ]}
        />
    );
}

export function PopularityChart({
    data,
    isLoading,
    isError,
    onRetry,
    className,
}: PopularityChartProps) {
    const { t } = useTranslation('instructorAnalytics');
    const colors = useChartColors();

    const rows = (data ?? []).slice(0, MAX_BARS);

    return (
        <ChartCard
            title={t('popularity.title')}
            className={className}
            isLoading={isLoading}
            isError={isError}
            onRetry={onRetry}
            isEmpty={rows.length === 0}
        >
            <ResponsiveContainer width="100%" height={Math.max(rows.length * 40, 120)}>
                <BarChart
                    data={rows}
                    layout="vertical"
                    margin={{ top: 0, right: 12, bottom: 0, left: 8 }}
                >
                    <CartesianGrid
                        horizontal={false}
                        stroke={colors.border}
                        strokeDasharray="3 3"
                    />
                    <XAxis
                        type="number"
                        allowDecimals={false}
                        stroke={colors.muted}
                        fontSize={11}
                        tickLine={false}
                        axisLine={false}
                    />
                    <YAxis
                        type="category"
                        dataKey="title"
                        width={140}
                        tick={{ fill: colors.muted, fontSize: 11 }}
                        tickLine={false}
                        axisLine={false}
                        tickFormatter={(value: string) =>
                            value.length > 22 ? `${value.slice(0, 21)}…` : value
                        }
                    />
                    <Tooltip
                        cursor={{ fill: colors.border, fillOpacity: 0.3 }}
                        content={
                            <PopularityTooltipContent
                                enrollmentsLabel={t('popularity.enrollments')}
                                color={colors.primary}
                            />
                        }
                    />
                    <Bar
                        dataKey="enrollments"
                        fill={colors.primary}
                        radius={[0, 4, 4, 0]}
                        barSize={18}
                    />
                </BarChart>
            </ResponsiveContainer>
        </ChartCard>
    );
}
