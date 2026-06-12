import { DollarSign, ShoppingCart } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { useInstructorEarningsQuery } from '@/hooks/useInstructorEarningsQuery';

export default function InstructorEarningsPage() {
    const { t } = useTranslation('instructor');
    const { data, isLoading } = useInstructorEarningsQuery();

    return (
        <div className="p-8">
            {/* Header */}
            <div className="mb-8 flex items-end justify-between">
                <div>
                    <h1 className="font-heading text-3xl font-bold text-foreground">
                        {t('earningsTitle')}
                    </h1>
                    <p className="mt-1 text-muted-foreground">{t('earningsSubtitle')}</p>
                </div>
                <span className="rounded-full border border-warning/30 bg-warning/10 px-3 py-1 text-xs font-medium text-warning">
                    {t('earningsFreeBadge')}
                </span>
            </div>

            {/* Stats */}
            <div className="mb-8 grid gap-4 md:grid-cols-2">
                <div className="flex items-center gap-4 rounded-xl border border-border bg-card p-5">
                    <div className="grid h-10 w-10 place-items-center rounded-lg bg-primary/10">
                        <DollarSign size={20} className="text-primary" />
                    </div>
                    <div>
                        <p className="text-sm text-muted-foreground">{t('earningsTotal')}</p>
                        <p className="font-heading text-2xl font-bold text-foreground">
                            {isLoading ? '—' : `$${(data?.totalEarnings ?? 0).toFixed(2)}`}
                        </p>
                    </div>
                </div>
                <div className="flex items-center gap-4 rounded-xl border border-border bg-card p-5">
                    <div className="grid h-10 w-10 place-items-center rounded-lg bg-primary/10">
                        <ShoppingCart size={20} className="text-primary" />
                    </div>
                    <div>
                        <p className="text-sm text-muted-foreground">{t('earningsPayments')}</p>
                        <p className="font-heading text-2xl font-bold text-foreground">
                            {isLoading ? '—' : (data?.totalPayments ?? 0).toLocaleString()}
                        </p>
                    </div>
                </div>
            </div>

            {/* Revenue by course */}
            <div className="overflow-hidden rounded-xl border border-border bg-card">
                <div className="border-b border-border px-5 py-4">
                    <h3 className="font-heading font-semibold text-foreground">
                        {t('earningsCoursesTableTitle')}
                    </h3>
                </div>

                {isLoading ? (
                    <div className="py-12 text-center text-sm text-muted-foreground">
                        Loading...
                    </div>
                ) : !data || data.courses.length === 0 ? (
                    <div className="py-12 text-center text-sm text-muted-foreground">
                        {t('earningsEmpty')}
                    </div>
                ) : (
                    <table className="w-full text-sm">
                        <thead className="bg-secondary/50 text-xs uppercase tracking-wider text-muted-foreground">
                            <tr>
                                <th className="px-5 py-3 text-left font-medium">
                                    {t('earningsColCourse')}
                                </th>
                                <th className="px-5 py-3 text-left font-medium">
                                    {t('earningsColPayments')}
                                </th>
                                <th className="px-5 py-3 text-left font-medium">
                                    {t('earningsColRevenue')}
                                </th>
                                <th className="px-5 py-3 text-left font-medium">
                                    {t('earningsColLast')}
                                </th>
                            </tr>
                        </thead>
                        <tbody className="divide-y divide-border">
                            {data.courses.map((course) => (
                                <tr key={course.courseId} className="hover:bg-secondary/30">
                                    <td className="px-5 py-3 font-medium text-foreground">
                                        {course.courseTitle}
                                    </td>
                                    <td className="px-5 py-3 text-muted-foreground">
                                        {course.paymentsCount}
                                    </td>
                                    <td className="px-5 py-3 font-semibold text-foreground">
                                        ${course.totalAmount.toFixed(2)}
                                    </td>
                                    <td className="px-5 py-3 text-muted-foreground">
                                        {new Date(course.lastPaymentAt).toLocaleDateString()}
                                    </td>
                                </tr>
                            ))}
                        </tbody>
                        <tfoot>
                            <tr className="border-t border-border">
                                <td
                                    colSpan={2}
                                    className="px-5 py-3 text-right text-sm text-muted-foreground"
                                >
                                    Total
                                </td>
                                <td className="px-5 py-3 font-bold text-foreground">
                                    ${data.totalEarnings.toFixed(2)}
                                </td>
                                <td />
                            </tr>
                        </tfoot>
                    </table>
                )}
            </div>
        </div>
    );
}
