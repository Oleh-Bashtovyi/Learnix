import { Link, NavLink, Outlet, useNavigate } from 'react-router-dom';
import { useQueryClient } from '@tanstack/react-query';
import {
    LayoutDashboard,
    Users,
    BookOpen,
    FileCheck,
    CreditCard,
    Tag,
    ArrowLeft,
    LogOut,
} from 'lucide-react';
import { Helmet } from 'react-helmet-async';
import { useTranslation } from 'react-i18next';
import { cn } from '@/utils/cn';
import { useAuthStore } from '@/store/auth.store';
import { authApi } from '@/api/auth.api';

export function AdminLayout() {
    const { t } = useTranslation('admin');
    const navigate = useNavigate();
    const { logout } = useAuthStore();
    const queryClient = useQueryClient();

    const navItems = [
        {
            to: '/admin',
            label: t('navDashboard'),
            icon: <LayoutDashboard size={16} />,
            end: true,
        },
        { to: '/admin/users', label: t('navUsers'), icon: <Users size={16} /> },
        { to: '/admin/courses', label: t('navCourses'), icon: <BookOpen size={16} /> },
        {
            to: '/admin/applications',
            label: t('navApplications'),
            icon: <FileCheck size={16} />,
        },
        { to: '/admin/payments', label: t('navPayments'), icon: <CreditCard size={16} /> },
        { to: '/admin/categories', label: t('navCategories'), icon: <Tag size={16} /> },
    ];

    function handleSignOut() {
        authApi.logout().catch(() => {});
        logout();
        queryClient.clear();
        navigate('/login');
    }

    return (
        <>
            <Helmet>
                <meta name="robots" content="noindex,nofollow" />
            </Helmet>
            <div className="grid h-screen grid-cols-[240px_1fr] overflow-hidden bg-background">
                {/* Sidebar */}
                <aside className="flex flex-col overflow-y-auto border-r border-border bg-card">
                    <div className="flex items-center gap-2 px-4 py-5">
                        <Link
                            to="/"
                            className="flex items-center gap-2 font-heading font-bold text-foreground"
                        >
                            <div className="grid h-8 w-8 place-items-center rounded-lg bg-destructive text-sm font-bold text-destructive-foreground">
                                A
                            </div>
                            <span>Learnix Admin</span>
                        </Link>
                    </div>

                    <div className="flex-1 px-3 py-2">
                        <p className="mb-2 px-2 text-xs uppercase tracking-wider text-muted-foreground">
                            Admin
                        </p>
                        <nav className="space-y-1 text-sm">
                            {navItems.map((item) => (
                                <NavLink
                                    key={item.to}
                                    to={item.to}
                                    end={item.end}
                                    className={({ isActive }) =>
                                        cn(
                                            'flex items-center gap-2.5 rounded-lg px-3 py-2 transition-colors',
                                            isActive
                                                ? 'bg-destructive/10 font-medium text-destructive'
                                                : 'text-foreground hover:bg-secondary',
                                        )
                                    }
                                >
                                    {item.icon}
                                    {item.label}
                                </NavLink>
                            ))}
                        </nav>
                    </div>

                    <div className="border-t border-border px-3 py-4">
                        <p className="mb-2 px-2 text-xs uppercase tracking-wider text-muted-foreground">
                            Account
                        </p>
                        <nav className="space-y-1 text-sm">
                            <Link
                                to="/"
                                className="flex items-center gap-2.5 rounded-lg px-3 py-2 text-foreground transition-colors hover:bg-secondary"
                            >
                                <ArrowLeft size={16} />
                                {t('navBackToSite')}
                            </Link>
                            <button
                                onClick={handleSignOut}
                                className="flex w-full items-center gap-2.5 rounded-lg px-3 py-2 text-muted-foreground transition-colors hover:bg-secondary hover:text-foreground"
                            >
                                <LogOut size={16} />
                                {t('navSignOut')}
                            </button>
                        </nav>
                    </div>
                </aside>

                {/* Main content */}
                <main className="overflow-y-auto">
                    <Outlet />
                </main>
            </div>
        </>
    );
}
