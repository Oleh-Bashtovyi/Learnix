import { useTranslation } from 'react-i18next';
import type { InstructorTestPerformanceItem } from '@/types/instructorAnalytics.types';
import { ChartCard } from './ChartCard';

interface TestPerformanceTableProps {
    data?: InstructorTestPerformanceItem[];
    isLoading?: boolean;
    isError?: boolean;
    onRetry?: () => void;
}

export function TestPerformanceTable({
    data,
    isLoading,
    isError,
    onRetry,
}: TestPerformanceTableProps) {
    const { t } = useTranslation('instructorAnalytics');

    return (
        <ChartCard
            title={t('tests.title')}
            isLoading={isLoading}
            isError={isError}
            onRetry={onRetry}
            isEmpty={!data || data.length === 0}
        >
            <div className="overflow-x-auto">
                <table className="w-full text-sm">
                    <thead>
                        <tr className="border-b border-border text-left text-xs text-muted-foreground">
                            <th className="pb-2 pr-3 font-medium">{t('tests.lesson')}</th>
                            <th className="pb-2 pr-3 font-medium">{t('tests.course')}</th>
                            <th className="pb-2 pr-3 text-right font-medium">
                                {t('tests.averageScore')}
                            </th>
                            <th className="pb-2 text-right font-medium">{t('tests.passRate')}</th>
                        </tr>
                    </thead>
                    <tbody className="divide-y divide-border">
                        {data?.map((row) => (
                            <tr key={`${row.courseId}-${row.lessonId}`}>
                                <td className="py-2 pr-3 font-medium text-foreground">
                                    {row.lessonTitle}
                                </td>
                                <td className="max-w-48 truncate py-2 pr-3 text-muted-foreground">
                                    {row.courseTitle}
                                </td>
                                <td className="py-2 pr-3 text-right tabular-nums text-foreground">
                                    {row.averageScore} / {row.maxScore}
                                </td>
                                <td className="py-2 text-right tabular-nums text-foreground">
                                    {Math.round(row.passRate * 100)}%
                                </td>
                            </tr>
                        ))}
                    </tbody>
                </table>
            </div>
        </ChartCard>
    );
}
