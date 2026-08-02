import type { ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import {
    Table,
    TableBody,
    TableCell,
    TableFooter,
    TableHead,
    TableHeader,
    TableRow,
} from '@/components/ui/table';
import type { CourseEarningsDto } from '@/types/payment.types';
import { ChartCard } from './ChartCard';

interface EarningsTableProps {
    data?: CourseEarningsDto[];
    total?: number;
    isLoading?: boolean;
    isError?: boolean;
    onRetry?: () => void;
    action?: ReactNode;
}

export function EarningsTable({
    data,
    total,
    isLoading,
    isError,
    onRetry,
    action,
}: EarningsTableProps) {
    const { t } = useTranslation('instructorAnalytics');

    return (
        <ChartCard
            title={t('earnings.tableTitle')}
            action={action}
            isLoading={isLoading}
            isError={isError}
            onRetry={onRetry}
            isEmpty={!data || data.length === 0}
            emptyMessage={t('earnings.empty')}
        >
            <Table>
                <TableHeader>
                    <TableRow className="bg-secondary/50 text-xs uppercase tracking-wider hover:bg-secondary/50">
                        <TableHead>{t('common:general.course')}</TableHead>
                        <TableHead>{t('earnings.colPayments')}</TableHead>
                        <TableHead>{t('earnings.colRevenue')}</TableHead>
                        <TableHead>{t('earnings.colLast')}</TableHead>
                    </TableRow>
                </TableHeader>
                <TableBody>
                    {data?.map((course) => (
                        <TableRow key={course.courseId}>
                            <TableCell className="font-medium text-foreground">
                                {course.courseTitle}
                            </TableCell>
                            <TableCell className="text-muted-foreground">
                                {course.paymentsCount}
                            </TableCell>
                            <TableCell className="font-semibold text-foreground">
                                ${course.totalAmount.toFixed(2)}
                            </TableCell>
                            <TableCell className="text-muted-foreground">
                                {new Date(course.lastPaymentAt).toLocaleDateString()}
                            </TableCell>
                        </TableRow>
                    ))}
                </TableBody>
                {data && data.length > 0 && (
                    <TableFooter>
                        <TableRow>
                            <TableCell colSpan={2} className="text-right text-muted-foreground">
                                {t('earnings.footerTotal')}
                            </TableCell>
                            <TableCell className="font-bold text-foreground">
                                ${(total ?? 0).toFixed(2)}
                            </TableCell>
                            <TableCell />
                        </TableRow>
                    </TableFooter>
                )}
            </Table>
        </ChartCard>
    );
}
