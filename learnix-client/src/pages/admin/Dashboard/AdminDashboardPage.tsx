import { Link } from 'react-router-dom';
import { Users, BookOpen, FileCheck, CreditCard } from 'lucide-react';
import { ADMIN } from '@/const/localization/admin';

const MOCK_STATS = [
    { label: ADMIN.STAT_TOTAL_USERS, value: '1,247', sub: 'Registered accounts' },
    { label: ADMIN.STAT_TOTAL_COURSES, value: '83', sub: '61 published, 22 draft' },
    { label: ADMIN.STAT_PENDING_APPS, value: '5', sub: 'Awaiting review' },
    { label: ADMIN.STAT_REVENUE, value: '$12,480', sub: 'Simulated total' },
];

const QUICK_LINKS = [
    {
        to: '/admin/users',
        icon: <Users size={24} />,
        title: ADMIN.QUICK_USERS_TITLE,
        desc: ADMIN.QUICK_USERS_DESC,
        color: 'text-primary bg-primary/10',
    },
    {
        to: '/admin/courses',
        icon: <BookOpen size={24} />,
        title: ADMIN.QUICK_COURSES_TITLE,
        desc: ADMIN.QUICK_COURSES_DESC,
        color: 'text-accent bg-accent/10',
    },
    {
        to: '/admin/applications',
        icon: <FileCheck size={24} />,
        title: ADMIN.QUICK_APPLICATIONS_TITLE,
        desc: ADMIN.QUICK_APPLICATIONS_DESC,
        color: 'text-success bg-success/10',
    },
    {
        to: '/admin/payments',
        icon: <CreditCard size={24} />,
        title: ADMIN.QUICK_PAYMENTS_TITLE,
        desc: ADMIN.QUICK_PAYMENTS_DESC,
        color: 'text-warning bg-warning/10',
    },
];

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
    return (
        <div className="p-8">
            <div className="mb-8">
                <h1 className="font-heading text-3xl font-bold text-foreground">
                    {ADMIN.DASHBOARD_TITLE}
                </h1>
                <p className="mt-1 text-muted-foreground">{ADMIN.DASHBOARD_SUBTITLE}</p>
            </div>

            {/* Stats */}
            <div className="mb-10 grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
                {MOCK_STATS.map((s) => (
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
