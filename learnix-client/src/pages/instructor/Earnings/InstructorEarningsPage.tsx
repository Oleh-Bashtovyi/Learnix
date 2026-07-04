import { useTranslation } from 'react-i18next';
import { DollarSign, ShoppingCart } from 'lucide-react';
import { Skeleton } from '@/components/ui/skeleton';
import {
    Table,
    TableBody,
    TableCell,
    TableFooter,
    TableHead,
    TableHeader,
    TableRow,
} from '@/components/ui/table';
import { useInstructorEarningsQuery } from '@/hooks/instructor/useInstructorEarningsQuery';
import { InstructorEarningsRow } from './components/InstructorEarningsRow';

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
                    <div className="grid size-10 place-items-center rounded-lg bg-primary/10">
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
                    <div className="grid size-10 place-items-center rounded-lg bg-primary/10">
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

                <Table>
                    <TableHeader>
                        <TableRow className="bg-secondary/50 text-xs uppercase tracking-wider hover:bg-secondary/50">
                            <TableHead>{t('earningsColCourse')}</TableHead>
                            <TableHead>{t('earningsColPayments')}</TableHead>
                            <TableHead>{t('earningsColRevenue')}</TableHead>
                            <TableHead>{t('earningsColLast')}</TableHead>
                        </TableRow>
                    </TableHeader>
                    <TableBody>
                        {isLoading ? (
                            Array.from({ length: 3 }).map((_, i) => (
                                <TableRow key={i}>
                                    <TableCell>
                                        <Skeleton className="h-6 w-48" />
                                    </TableCell>
                                    <TableCell>
                                        <Skeleton className="h-6 w-10" />
                                    </TableCell>
                                    <TableCell>
                                        <Skeleton className="h-6 w-16" />
                                    </TableCell>
                                    <TableCell>
                                        <Skeleton className="h-6 w-24" />
                                    </TableCell>
                                </TableRow>
                            ))
                        ) : !data || data.courses.length === 0 ? (
                            <TableRow>
                                <TableCell
                                    colSpan={4}
                                    className="py-16 text-center text-muted-foreground"
                                >
                                    {t('earningsEmpty')}
                                </TableCell>
                            </TableRow>
                        ) : (
                            data.courses.map((course) => (
                                <InstructorEarningsRow key={course.courseId} course={course} />
                            ))
                        )}
                    </TableBody>
                    {!isLoading && data && data.courses.length > 0 && (
                        <TableFooter>
                            <TableRow>
                                <TableCell colSpan={2} className="text-right text-muted-foreground">
                                    Total
                                </TableCell>
                                <TableCell className="font-bold text-foreground">
                                    ${data.totalEarnings.toFixed(2)}
                                </TableCell>
                                <TableCell />
                            </TableRow>
                        </TableFooter>
                    )}
                </Table>
            </div>
        </div>
    );
}
