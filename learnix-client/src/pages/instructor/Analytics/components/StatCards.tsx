import { useTranslation } from 'react-i18next';
import { Award, BookOpen, DollarSign, Star, Users } from 'lucide-react';
import { StatTile, type StatTrend } from '@/components/common/elements/StatTile';
import { StatValueSkeleton } from '@/components/common/elements/StatValueSkeleton';
import { TREND_WINDOW_DAYS } from '@/const/analytics.constants';
import type {
    InstructorAnalyticsSummary,
    InstructorAnalyticsTrend,
} from '@/types/instructorAnalytics.types';

interface StatCardsProps {
    summary?: InstructorAnalyticsSummary;
    /** Every course the instructor owns, whatever its status. */
    coursesCount?: number;
    isLoading?: boolean;
}

export function StatCards({ summary, coursesCount, isLoading }: StatCardsProps) {
    const { t } = useTranslation('instructorAnalytics');

    const currency = (value: number) =>
        value.toLocaleString(undefined, {
            style: 'currency',
            currency: 'USD',
            maximumFractionDigits: 0,
        });

    // A window that added nothing AND has no baseline to compare against gets no `delta` at all —
    // "+0" next to a total with nothing to say is noise. But a window that dropped to zero *from* a
    // real baseline (changePercent is not null) still shows "0": a bare "↓100%" with no count beside
    // it reads as "you lost everything", when it actually means "nothing new this window" — the
    // number is what disambiguates the two. No leading "+" in that case; going from something to
    // nothing isn't an addition.
    function trend(
        source: InstructorAnalyticsTrend | undefined,
        format: (value: number) => string,
    ): StatTrend | undefined {
        if (!source) return undefined;

        const delta =
            source.current > 0
                ? `+${format(source.current)}`
                : source.changePercent !== null
                  ? format(0)
                  : undefined;

        return {
            changePercent: source.changePercent,
            delta,
            title: t('stats.trendTitle', { days: TREND_WINDOW_DAYS }),
        };
    }

    return (
        <dl className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-5">
            <StatTile
                icon={<BookOpen className="size-5" />}
                tone="neutral"
                surface="card"
                label={t('stats.courses')}
                value={isLoading ? <StatValueSkeleton /> : (coursesCount ?? 0).toLocaleString()}
            />
            <StatTile
                icon={<Users className="size-5" />}
                tone="accent"
                surface="card"
                label={t('stats.students')}
                value={
                    isLoading ? (
                        <StatValueSkeleton />
                    ) : (
                        (summary?.totalStudents ?? 0).toLocaleString()
                    )
                }
                trend={trend(summary?.newStudentsTrend, (v) => v.toLocaleString())}
            />
            <StatTile
                icon={<DollarSign className="size-5" />}
                tone="brand"
                surface="card"
                label={t('stats.revenue')}
                value={isLoading ? <StatValueSkeleton /> : currency(summary?.totalRevenue ?? 0)}
                trend={trend(summary?.revenueTrend, currency)}
            />
            <StatTile
                icon={<Star className="size-5" />}
                tone="warning"
                surface="card"
                label={t('stats.averageRating')}
                value={isLoading ? <StatValueSkeleton /> : (summary?.averageRating ?? 0).toFixed(2)}
            />
            <StatTile
                icon={<Award className="size-5" />}
                tone="success"
                surface="card"
                label={t('stats.certificates')}
                value={
                    isLoading ? (
                        <StatValueSkeleton />
                    ) : (
                        (summary?.certificatesIssued ?? 0).toLocaleString()
                    )
                }
                trend={trend(summary?.certificatesTrend, (v) => v.toLocaleString())}
            />
        </dl>
    );
}
