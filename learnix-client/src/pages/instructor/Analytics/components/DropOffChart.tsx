import { useTranslation } from 'react-i18next';
import {
    Area,
    AreaChart,
    CartesianGrid,
    ResponsiveContainer,
    Tooltip,
    XAxis,
    YAxis,
} from 'recharts';
import type { LessonDropOff } from '@/types/instructorAnalytics.types';
import { useChartColors } from '../useChartColors';
import { ChartCard } from './ChartCard';
import { ChartTooltip } from './ChartTooltip';
import { CourseFilter } from './CourseFilter';

interface CourseOption {
    id: string;
    title: string;
}

interface DropOffChartProps {
    courses: CourseOption[];
    courseId: string;
    onCourseChange: (courseId: string) => void;
    data?: LessonDropOff;
    isLoading?: boolean;
    isError?: boolean;
    onRetry?: () => void;
}

export function DropOffChart({
    courses,
    courseId,
    onCourseChange,
    data,
    isLoading,
    isError,
    onRetry,
}: DropOffChartProps) {
    const { t } = useTranslation('instructorAnalytics');
    const colors = useChartColors();

    const enrolled = data?.enrolled ?? 0;
    const rows = (data?.lessons ?? []).map((lesson, i) => ({
        index: i + 1,
        title: lesson.lessonTitle,
        completed: lesson.completed,
        pct: enrolled > 0 ? Math.round((lesson.completed / enrolled) * 100) : 0,
    }));

    const filter = (
        <CourseFilter
            courses={courses}
            value={courseId}
            onChange={onCourseChange}
            includeAll={false}
            disabled={courses.length === 0}
        />
    );

    return (
        <ChartCard
            title={t('dropOff.title')}
            action={filter}
            isLoading={isLoading}
            isError={isError}
            onRetry={onRetry}
            isEmpty={rows.length === 0}
        >
            <ResponsiveContainer width="100%" height={260}>
                <AreaChart data={rows} margin={{ top: 8, right: 12, bottom: 0, left: -8 }}>
                    <defs>
                        <linearGradient id="dropOffFill" x1="0" y1="0" x2="0" y2="1">
                            <stop offset="0%" stopColor={colors.accent} stopOpacity={0.3} />
                            <stop offset="100%" stopColor={colors.accent} stopOpacity={0} />
                        </linearGradient>
                    </defs>
                    <CartesianGrid vertical={false} stroke={colors.border} strokeDasharray="3 3" />
                    <XAxis
                        dataKey="index"
                        stroke={colors.muted}
                        fontSize={11}
                        tickLine={false}
                        axisLine={false}
                        minTickGap={16}
                    />
                    <YAxis
                        domain={[0, 100]}
                        ticks={[0, 25, 50, 75, 100]}
                        tickFormatter={(v: number) => `${v}%`}
                        width={40}
                        stroke={colors.muted}
                        fontSize={11}
                        tickLine={false}
                        axisLine={false}
                    />
                    <Tooltip
                        cursor={{ stroke: colors.border }}
                        content={({ active, payload }) => {
                            const point = payload?.[0]?.payload;
                            return (
                                <ChartTooltip
                                    active={active}
                                    title={point ? `${point.index}. ${point.title}` : undefined}
                                    rows={
                                        point
                                            ? [
                                                  {
                                                      label: t('dropOff.completion'),
                                                      value: `${point.pct}%`,
                                                      color: colors.accent,
                                                  },
                                                  {
                                                      label: t('dropOff.students'),
                                                      value: `${point.completed} / ${enrolled}`,
                                                  },
                                              ]
                                            : []
                                    }
                                />
                            );
                        }}
                    />
                    <Area
                        type="monotone"
                        dataKey="pct"
                        stroke={colors.accent}
                        strokeWidth={2}
                        fill="url(#dropOffFill)"
                        dot={{ r: 2, fill: colors.accent, strokeWidth: 0 }}
                    />
                </AreaChart>
            </ResponsiveContainer>
        </ChartCard>
    );
}
