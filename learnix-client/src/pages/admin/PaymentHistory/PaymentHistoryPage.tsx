import { useState } from 'react';
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
import type { MockPaymentDto } from '@/types/admin.types';
import { cn } from '@/utils/cn';
import { MOCK_PAYMENTS } from '@/utils/mocks/payments.mock';
import { PaymentHistoryRow } from './components/PaymentHistoryRow';

type StatusFilter = 'All' | 'Completed' | 'Pending' | 'Failed';

export default function PaymentHistoryPage() {
    const { t } = useTranslation('admin');
    const [filter, setFilter] = useState<StatusFilter>('All');

    const STATUS_LABELS: Record<MockPaymentDto['status'], string> = {
        Completed: t('payStatusCompleted'),
        Pending: t('payStatusPending'),
        Failed: t('payStatusFailed'),
    };

    const FILTERS: { value: StatusFilter; label: string }[] = [
        { value: 'All', label: t('payFilterAll') },
        { value: 'Completed', label: t('payFilterCompleted') },
        { value: 'Pending', label: t('payFilterPending') },
        { value: 'Failed', label: t('payFilterFailed') },
    ];

    const displayed =
        filter === 'All' ? MOCK_PAYMENTS : MOCK_PAYMENTS.filter((p) => p.status === filter);

    const total = displayed.reduce(
        (sum, p) => (p.status === 'Completed' ? sum + p.amount : sum),
        0,
    );

    return (
        <div className="flex h-full flex-col p-8">
            {/* Header */}
            <div className="mb-2 flex flex-wrap items-start justify-between gap-4">
                <div>
                    <h1 className="font-heading text-3xl font-bold text-foreground">
                        {t('paymentsTitle')}
                    </h1>
                    <p className="mt-1 text-muted-foreground">{t('paymentsSubtitle')}</p>
                </div>
                <span className="rounded-full border border-warning/30 bg-warning/10 px-3 py-1 text-xs font-medium text-warning">
                    {t('paymentsMockBadge')}
                </span>
            </div>

            {/* Filters */}
            <div className="mb-4 mt-6 flex gap-2">
                {FILTERS.map((f) => (
                    <button
                        key={f.value}
                        onClick={() => setFilter(f.value)}
                        className={cn(
                            'rounded-lg px-3 py-1.5 text-sm transition-colors',
                            filter === f.value
                                ? 'bg-primary text-primary-foreground'
                                : 'bg-secondary text-foreground hover:bg-secondary/80',
                        )}
                    >
                        {f.label}
                    </button>
                ))}
            </div>

            <div className="flex min-h-0 flex-1 flex-col overflow-hidden rounded-xl border border-border bg-card">
                <div className="min-h-0 flex-1 overflow-y-auto">
                    <Table>
                        <TableHeader className="sticky top-0 bg-secondary/50 text-xs uppercase tracking-wider">
                            <TableRow>
                                <TableHead>{t('colPayer')}</TableHead>
                                <TableHead>{t('colCourseTitle')}</TableHead>
                                <TableHead>{t('colAmount')}</TableHead>
                                <TableHead>{t('colPayStatus')}</TableHead>
                                <TableHead>{t('colDate')}</TableHead>
                            </TableRow>
                        </TableHeader>
                        <TableBody>
                            {displayed.length === 0 ? (
                                <TableRow>
                                    <TableCell
                                        colSpan={5}
                                        className="py-16 text-center text-muted-foreground"
                                    >
                                        {t('emptyPayments')}
                                    </TableCell>
                                </TableRow>
                            ) : (
                                displayed.map((p) => (
                                    <PaymentHistoryRow
                                        key={p.id}
                                        payment={p}
                                        statusLabel={STATUS_LABELS[p.status]}
                                    />
                                ))
                            )}
                        </TableBody>
                        {displayed.length > 0 && filter !== 'Failed' && (
                            <TableFooter className="sticky bottom-0 bg-card">
                                <TableRow>
                                    <TableCell
                                        colSpan={4}
                                        className="text-right text-muted-foreground"
                                    >
                                        Completed total:
                                    </TableCell>
                                    <TableCell className="font-semibold text-foreground">
                                        ${total.toFixed(2)}
                                    </TableCell>
                                </TableRow>
                            </TableFooter>
                        )}
                    </Table>
                </div>
            </div>
        </div>
    );
}
