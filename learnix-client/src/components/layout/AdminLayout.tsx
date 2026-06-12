import { useState, useEffect } from 'react';
import { Link, NavLink, Outlet, useNavigate, useLocation } from 'react-router-dom';
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
    Sun,
    Moon,
    Menu,
    X,
} from 'lucide-react';
import { Helmet } from 'react-helmet-async';
import { useTranslation } from 'react-i18next';
import { cn } from '@/utils/cn';
import { useAuthStore } from '@/store/auth.store';
import { useThemeStore } from '@/store/theme.store';
import { authApi } from '@/api/auth.api';
import { LanguageSwitcher } from '@/components/common/LanguageSwitcher';

export function AdminLayout() {
    const { t } = useTranslation('admin');
    const navigate = useNavigate();
    const { logout } = useAuthStore();
    const queryClient = useQueryClient();
    const { theme, toggleTheme } = useThemeStore();
    const location = useLocation();
    const [mobileOpen, setMobileOpen] = useState(false);

    useEffect(() => {
        setMobileOpen(false);
    }, [location.pathname]);

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
            <div className="flex h-screen flex-col overflow-hidden bg-background md:grid md:grid-cols-[240px_1fr]">
                {/* Mobile header */}
                <div className="flex h-14 shrink-0 items-center justify-between border-b border-border bg-card px-4 md:hidden">
                    <Link
                        to="/"
                        className="flex items-center gap-2 font-heading font-bold text-foreground"
                    >
                        <div className="grid h-8 w-8 place-items-center rounded-lg bg-destructive text-sm font-bold text-destructive-foreground">
                            A
                        </div>
                        <span className="tracking-tight">Learnix Admin</span>
                    </Link>
                    <button
                        onClick={() => setMobileOpen(!mobileOpen)}
                        className="p-2 text-muted-foreground transition-colors hover:text-foreground"
                    >
                        {mobileOpen ? <X size={20} /> : <Menu size={20} />}
                    </button>
                </div>

                {/* Sidebar */}
                <aside
                    className={cn(
                        'fixed inset-0 top-14 z-40 flex flex-col overflow-y-auto border-r border-border bg-card transition-transform duration-200 md:static md:top-0 md:translate-x-0',
                        mobileOpen ? 'translate-x-0' : '-translate-x-full',
                    )}
                >
                    <div className="hidden items-center gap-2 px-4 py-5 md:flex">
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
                        <div className="mt-4 flex items-center justify-between px-2">
                            <LanguageSwitcher />
                            <button
                                type="button"
                                onClick={toggleTheme}
                                aria-label="Toggle theme"
                                className="grid h-9 w-9 place-items-center rounded-lg text-muted-foreground transition-colors hover:bg-secondary hover:text-foreground"
                            >
                                {theme === 'dark' ? <Sun size={16} /> : <Moon size={16} />}
                            </button>
                        </div>
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
