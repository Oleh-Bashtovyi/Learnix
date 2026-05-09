import { useState } from 'react';
import { ADMIN } from '@/const/localization/admin';
import { cn } from '@/utils/cn';
import type { MockPaymentDto } from '@/types/admin.types';

const MOCK_PAYMENTS: MockPaymentDto[] = [
    {
        id: '1',
        userName: 'Alice Johnson',
        userEmail: 'alice@example.com',
        courseTitle: 'Advanced React Patterns',
        amount: 49.99,
        status: 'Completed',
        createdAt: '2025-04-28T10:14:00Z',
    },
    {
        id: '2',
        userName: 'Bob Martinez',
        userEmail: 'bob@example.com',
        courseTitle: 'Node.js Masterclass',
        amount: 34.99,
        status: 'Completed',
        createdAt: '2025-04-27T15:32:00Z',
    },
    {
        id: '3',
        userName: 'Carol Williams',
        userEmail: 'carol@example.com',
        courseTitle: 'TypeScript Deep Dive',
        amount: 39.99,
        status: 'Pending',
        createdAt: '2025-04-27T09:05:00Z',
    },
    {
        id: '4',
        userName: 'David Lee',
        userEmail: 'david@example.com',
        courseTitle: 'Docker & Kubernetes',
        amount: 59.99,
        status: 'Completed',
        createdAt: '2025-04-26T18:20:00Z',
    },
    {
        id: '5',
        userName: 'Eva Chen',
        userEmail: 'eva@example.com',
        courseTitle: 'Python for Data Science',
        amount: 44.99,
        status: 'Failed',
        createdAt: '2025-04-26T11:48:00Z',
    },
    {
        id: '6',
        userName: 'Frank Brown',
        userEmail: 'frank@example.com',
        courseTitle: 'AWS Cloud Practitioner',
        amount: 29.99,
        status: 'Completed',
        createdAt: '2025-04-25T14:00:00Z',
    },
    {
        id: '7',
        userName: 'Grace Kim',
        userEmail: 'grace@example.com',
        courseTitle: 'GraphQL Fundamentals',
        amount: 24.99,
        status: 'Completed',
        createdAt: '2025-04-25T09:30:00Z',
    },
    {
        id: '8',
        userName: 'Henry Taylor',
        userEmail: 'henry@example.com',
        courseTitle: 'Advanced React Patterns',
        amount: 49.99,
        status: 'Failed',
        createdAt: '2025-04-24T20:15:00Z',
    },
    {
        id: '9',
        userName: 'Iris Walker',
        userEmail: 'iris@example.com',
        courseTitle: 'Node.js Masterclass',
        amount: 34.99,
        status: 'Completed',
        createdAt: '2025-04-24T16:40:00Z',
    },
    {
        id: '10',
        userName: 'Jack Wilson',
        userEmail: 'jack@example.com',
        courseTitle: 'Docker & Kubernetes',
        amount: 59.99,
        status: 'Pending',
        createdAt: '2025-04-23T13:05:00Z',
    },
    {
        id: '11',
        userName: 'Karen Davis',
        userEmail: 'karen@example.com',
        courseTitle: 'TypeScript Deep Dive',
        amount: 39.99,
        status: 'Completed',
        createdAt: '2025-04-23T08:55:00Z',
    },
    {
        id: '12',
        userName: 'Liam Garcia',
        userEmail: 'liam@example.com',
        courseTitle: 'Python for Data Science',
        amount: 44.99,
        status: 'Completed',
        createdAt: '2025-04-22T17:30:00Z',
    },
];

type StatusFilter = 'All' | 'Completed' | 'Pending' | 'Failed';

const STATUS_STYLES: Record<MockPaymentDto['status'], string> = {
    Completed: 'bg-success/20 text-success',
    Pending: 'bg-warning/20 text-warning',
    Failed: 'bg-destructive/10 text-destructive',
};

const STATUS_LABELS: Record<MockPaymentDto['status'], string> = {
    Completed: ADMIN.PAY_STATUS_COMPLETED,
    Pending: ADMIN.PAY_STATUS_PENDING,
    Failed: ADMIN.PAY_STATUS_FAILED,
};

const FILTERS: { value: StatusFilter; label: string }[] = [
    { value: 'All', label: ADMIN.PAY_FILTER_ALL },
    { value: 'Completed', label: ADMIN.PAY_FILTER_COMPLETED },
    { value: 'Pending', label: ADMIN.PAY_FILTER_PENDING },
    { value: 'Failed', label: ADMIN.PAY_FILTER_FAILED },
];

export default function PaymentHistoryPage() {
    const [filter, setFilter] = useState<StatusFilter>('All');

    const displayed =
        filter === 'All' ? MOCK_PAYMENTS : MOCK_PAYMENTS.filter((p) => p.status === filter);

    const total = displayed.reduce(
        (sum, p) => (p.status === 'Completed' ? sum + p.amount : sum),
        0,
    );

    return (
        <div className="p-8">
            {/* Header */}
            <div className="mb-2 flex flex-wrap items-start justify-between gap-4">
                <div>
                    <h1 className="font-heading text-3xl font-bold text-foreground">
                        {ADMIN.PAYMENTS_TITLE}
                    </h1>
                    <p className="mt-1 text-muted-foreground">{ADMIN.PAYMENTS_SUBTITLE}</p>
                </div>
                <span className="rounded-full border border-warning/30 bg-warning/10 px-3 py-1 text-xs font-medium text-warning">
                    {ADMIN.PAYMENTS_MOCK_BADGE}
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

            {/* Table */}
            <div className="overflow-hidden rounded-xl border border-border bg-card">
                {displayed.length === 0 ? (
                    <div className="py-16 text-center text-sm text-muted-foreground">
                        {ADMIN.EMPTY_PAYMENTS}
                    </div>
                ) : (
                    <table className="w-full text-sm">
                        <thead className="bg-secondary/50 text-xs uppercase tracking-wider text-muted-foreground">
                            <tr>
                                <th className="px-5 py-3 text-left font-medium">
                                    {ADMIN.COL_PAYER}
                                </th>
                                <th className="px-5 py-3 text-left font-medium">
                                    {ADMIN.COL_COURSE_TITLE}
                                </th>
                                <th className="px-5 py-3 text-left font-medium">
                                    {ADMIN.COL_AMOUNT}
                                </th>
                                <th className="px-5 py-3 text-left font-medium">
                                    {ADMIN.COL_PAY_STATUS}
                                </th>
                                <th className="px-5 py-3 text-left font-medium">
                                    {ADMIN.COL_DATE}
                                </th>
                            </tr>
                        </thead>
                        <tbody className="divide-y divide-border">
                            {displayed.map((p) => (
                                <tr key={p.id} className="hover:bg-secondary/30">
                                    {/* User */}
                                    <td className="px-5 py-3">
                                        <p className="font-medium text-foreground">{p.userName}</p>
                                        <p className="text-xs text-muted-foreground">
                                            {p.userEmail}
                                        </p>
                                    </td>
                                    {/* Course */}
                                    <td className="px-5 py-3 text-foreground">{p.courseTitle}</td>
                                    {/* Amount */}
                                    <td className="px-5 py-3 font-medium text-foreground">
                                        ${p.amount.toFixed(2)}
                                    </td>
                                    {/* Status */}
                                    <td className="px-5 py-3">
                                        <span
                                            className={cn(
                                                'rounded px-2 py-0.5 text-xs font-medium',
                                                STATUS_STYLES[p.status],
                                            )}
                                        >
                                            {STATUS_LABELS[p.status]}
                                        </span>
                                    </td>
                                    {/* Date */}
                                    <td className="px-5 py-3 text-muted-foreground">
                                        {new Date(p.createdAt).toLocaleDateString()}
                                    </td>
                                </tr>
                            ))}
                        </tbody>
                    </table>
                )}

                {/* Summary */}
                {displayed.length > 0 && filter !== 'Failed' && (
                    <div className="flex justify-end border-t border-border px-5 py-3">
                        <p className="text-sm text-muted-foreground">
                            Completed total:{' '}
                            <span className="font-semibold text-foreground">
                                ${total.toFixed(2)}
                            </span>
                        </p>
                    </div>
                )}
            </div>
        </div>
    );
}
