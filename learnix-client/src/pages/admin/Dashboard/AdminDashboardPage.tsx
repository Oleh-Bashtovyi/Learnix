import { Link } from 'react-router-dom';
import { Users, BookOpen, FileCheck, CreditCard } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { adminApi } from '@/api/admin.api';
import { queryKeys } from '@/api/queryKeys';

function StatCard({ label, value, sub }: { label: string; value: string; sub: string }) {
    return (
        <div className="rounded-xl border border-border bg-card p-5">
            <p className="text-sm text-muted-foreground">{label}</p>
            <p className="mt-1 font-heading text-3xl font-bold text-foreground">{value}</p>
            <p className="mt-2 text-xs text-muted-foreground">{sub}</p>
        </div>
    );
}

export default function AdminDashboardPage() {
    const { t } = useTranslation('admin');

    const { data: stats } = useQuery({
        queryKey: queryKeys.admin.stats(),
        queryFn: adminApi.getStats,
    });

    const statCards = [
        {
            label: t('statTotalUsers'),
            value: stats ? stats.totalUsers.toLocaleString() : '—',
            sub: t('statTotalUsersSub'),
        },
        {
            label: t('statTotalCourses'),
            value: stats ? stats.totalCourses.toLocaleString() : '—',
            sub: stats
                ? t('statTotalCoursesSub', {
                      published: stats.publishedCourses,
                      draft: stats.draftCourses,
                  })
                : '—',
        },
        {
            label: t('statPendingApps'),
            value: stats ? stats.pendingApplications.toLocaleString() : '—',
            sub: t('statPendingAppsSub'),
        },
        {
            label: t('statRevenue'),
            value: '$12,480',
            sub: t('statRevenueSub'),
        },
    ];

    const QUICK_LINKS = [
        {
            to: '/admin/users',
            icon: <Users size={24} />,
            title: t('quickUsersTitle'),
            desc: t('quickUsersDesc'),
            color: 'text-primary bg-primary/10',
        },
        {
            to: '/admin/courses',
            icon: <BookOpen size={24} />,
            title: t('quickCoursesTitle'),
            desc: t('quickCoursesDesc'),
            color: 'text-accent bg-accent/10',
        },
        {
            to: '/admin/applications',
            icon: <FileCheck size={24} />,
            title: t('quickApplicationsTitle'),
            desc: t('quickApplicationsDesc'),
            color: 'text-success bg-success/10',
        },
        {
            to: '/admin/payments',
            icon: <CreditCard size={24} />,
            title: t('quickPaymentsTitle'),
            desc: t('quickPaymentsDesc'),
            color: 'text-warning bg-warning/10',
        },
    ];

    return (
        <div className="p-8">
            <div className="mb-8">
                <h1 className="font-heading text-3xl font-bold text-foreground">
                    {t('dashboardTitle')}
                </h1>
                <p className="mt-1 text-muted-foreground">{t('dashboardSubtitle')}</p>
            </div>

            {/* Stats */}
            <div className="mb-10 grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
                {statCards.map((s) => (
                    <StatCard key={s.label} {...s} />
                ))}
            </div>

            {/* Quick links */}
            <div className="grid gap-4 sm:grid-cols-2">
                {QUICK_LINKS.map((ql) => (
                    <Link
                        key={ql.to}
                        to={ql.to}
                        className="flex items-start gap-4 rounded-xl border border-border bg-card p-6 transition-shadow hover:shadow-md"
                    >
                        <div className={`rounded-lg p-3 ${ql.color}`}>{ql.icon}</div>
                        <div>
                            <h3 className="font-heading font-semibold text-foreground">
                                {ql.title}
                            </h3>
                            <p className="mt-1 text-sm text-muted-foreground">{ql.desc}</p>
                        </div>
                    </Link>
                ))}
            </div>
        </div>
    );
}
