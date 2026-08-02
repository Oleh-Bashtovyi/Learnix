import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import {
    Area,
    AreaChart,
    CartesianGrid,
    ResponsiveContainer,
    Tooltip,
    type TooltipContentProps,
    XAxis,
    YAxis,
} from 'recharts';
import { useInstructorDynamics } from '@/hooks/instructor/useInstructorAnalytics';
import { cn } from '@/utils/cn';
import { useChartColors } from '../useChartColors';
import { ChartCard } from './ChartCard';
import { ChartTooltip } from './ChartTooltip';

const RANGE_DAYS = [7, 30, 90] as const;
type RangeDays = (typeof RANGE_DAYS)[number];

function toDateParam(date: Date): string {
    return date.toISOString().slice(0, 10);
}

function formatAxisDate(value: string): string {
    // yyyy-MM-dd → MM/dd, locale-agnostic and compact
    const [, month, day] = value.split('-');
    return `${month}/${day}`;
}

interface DynamicsTooltipContentProps extends Partial<TooltipContentProps<number, string>> {
    valueLabel: string;
    color: string;
    formatValue?: (value: number) => string;
}

function DynamicsTooltipContent({
    active,
    payload,
    label,
    valueLabel,
    color,
    formatValue,
}: DynamicsTooltipContentProps) {
    const raw = Number(payload?.[0]?.value ?? 0);

    return (
        <ChartTooltip
            active={active}
            title={label as string}
            rows={[
                {
                    label: valueLabel,
                    value: formatValue ? formatValue(raw) : raw,
                    color,
                },
            ]}
        />
    );
}

export function DynamicsChart() {
    const { t } = useTranslation('instructorAnalytics');
    const colors = useChartColors();
    const [rangeDays, setRangeDays] = useState<RangeDays>(30);
    const [cumulative, setCumulative] = useState(false);

    const { startDate, endDate } = useMemo(() => {
        const end = new Date();
        const start = new Date();
        start.setDate(end.getDate() - (rangeDays - 1));
        return { startDate: toDateParam(start), endDate: toDateParam(end) };
    }, [rangeDays]);

    const { data, isLoading, isFetching, isError, refetch } = useInstructorDynamics(
        startDate,
        endDate,
    );

    // "Cumulative" turns the daily bars into running totals, so a slow trickle still reads as growth.
    const chartData = useMemo(() => {
        if (!data || !cumulative) return data;
        let enrollments = 0;
        let earnings = 0;
        return data.map((d) => {
            enrollments += d.enrollments;
            earnings += d.earnings;
            return { ...d, enrollments, earnings };
        });
    }, [data, cumulative]);

    const currency = (value: number) =>
        value.toLocaleString(undefined, {
            style: 'currency',
            currency: 'USD',
            maximumFractionDigits: 0,
        });

    const headerActions = (
        <div className="flex items-center gap-2">
            <button
                type="button"
                onClick={() => setCumulative((v) => !v)}
                className={cn(
                    'rounded-md px-2.5 py-1 text-xs font-medium transition-colors',
                    cumulative
                        ? 'bg-primary text-primary-foreground'
                        : 'text-muted-foreground hover:bg-secondary',
                )}
            >
                {t('dynamics.cumulative')}
            </button>
            <div className="flex gap-1">
                {RANGE_DAYS.map((days) => (
                    <button
                        key={days}
                        type="button"
                        onClick={() => setRangeDays(days)}
                        className={cn(
                            'rounded-md px-2.5 py-1 text-xs font-medium transition-colors',
                            rangeDays === days
                                ? 'bg-primary text-primary-foreground'
                                : 'text-muted-foreground hover:bg-secondary',
                        )}
                    >
                        {t(`dynamics.range${days}d`)}
                    </button>
                ))}
            </div>
        </div>
    );

    const axisProps = {
        stroke: colors.muted,
        fontSize: 11,
        tickLine: false,
        axisLine: false,
    };

    return (
        <ChartCard
            title={t('dynamics.title')}
            action={headerActions}
            isLoading={isLoading}
            isError={isError}
            onRetry={() => refetch()}
            isEmpty={!data || data.length === 0}
        >
            {/* Two separate plots that share the x-axis — enrollments (count) and revenue (money) live on
                different scales, so they never share one dual y-axis. */}
            <div
                className={cn(
                    'space-y-5 transition-opacity',
                    isFetching && !isLoading && 'opacity-60',
                )}
            >
                <div>
                    <p className="mb-1 text-xs text-muted-foreground">
                        {t('dynamics.enrollments')}
                    </p>
                    <ResponsiveContainer width="100%" height={160}>
                        <AreaChart
                            data={chartData}
                            margin={{ top: 4, right: 8, bottom: 0, left: -8 }}
                        >
                            <defs>
                                <linearGradient id="enrollFill" x1="0" y1="0" x2="0" y2="1">
                                    <stop
                                        offset="0%"
                                        stopColor={colors.primary}
                                        stopOpacity={0.3}
                                    />
                                    <stop
                                        offset="100%"
                                        stopColor={colors.primary}
                                        stopOpacity={0}
                                    />
                                </linearGradient>
                            </defs>
                            <CartesianGrid
                                vertical={false}
                                stroke={colors.border}
                                strokeDasharray="3 3"
                            />
                            <XAxis
                                dataKey="date"
                                tickFormatter={formatAxisDate}
                                minTickGap={24}
                                {...axisProps}
                            />
                            <YAxis allowDecimals={false} width={44} {...axisProps} />
                            <Tooltip
                                cursor={{ stroke: colors.border }}
                                content={
                                    <DynamicsTooltipContent
                                        valueLabel={t('dynamics.enrollments')}
                                        color={colors.primary}
                                    />
                                }
                            />
                            <Area
                                type="monotone"
                                dataKey="enrollments"
                                stroke={colors.primary}
                                strokeWidth={2}
                                fill="url(#enrollFill)"
                            />
                        </AreaChart>
                    </ResponsiveContainer>
                </div>

                <div>
                    <p className="mb-1 text-xs text-muted-foreground">{t('dynamics.revenue')}</p>
                    <ResponsiveContainer width="100%" height={160}>
                        <AreaChart
                            data={chartData}
                            margin={{ top: 4, right: 8, bottom: 0, left: -8 }}
                        >
                            <defs>
                                <linearGradient id="revenueFill" x1="0" y1="0" x2="0" y2="1">
                                    <stop offset="0%" stopColor={colors.accent} stopOpacity={0.3} />
                                    <stop offset="100%" stopColor={colors.accent} stopOpacity={0} />
                                </linearGradient>
                            </defs>
                            <CartesianGrid
                                vertical={false}
                                stroke={colors.border}
                                strokeDasharray="3 3"
                            />
                            <XAxis
                                dataKey="date"
                                tickFormatter={formatAxisDate}
                                minTickGap={24}
                                {...axisProps}
                            />
                            <YAxis width={44} {...axisProps} />
                            <Tooltip
                                cursor={{ stroke: colors.border }}
                                content={
                                    <DynamicsTooltipContent
                                        valueLabel={t('dynamics.revenue')}
                                        color={colors.accent}
                                        formatValue={currency}
                                    />
                                }
                            />
                            <Area
                                type="monotone"
                                dataKey="earnings"
                                stroke={colors.accent}
                                strokeWidth={2}
                                fill="url(#revenueFill)"
                            />
                        </AreaChart>
                    </ResponsiveContainer>
                </div>
            </div>
        </ChartCard>
    );
}
