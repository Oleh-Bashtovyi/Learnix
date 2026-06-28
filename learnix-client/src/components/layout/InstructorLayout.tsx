import { useState } from 'react';
import { Helmet } from 'react-helmet-async';
import { useTranslation } from 'react-i18next';
import { Link, NavLink, Outlet, useLocation, useNavigate } from 'react-router-dom';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import {
    ArrowLeft,
    BookOpen,
    LayoutDashboard,
    LogOut,
    Menu,
    MessageSquare,
    Moon,
    PlusCircle,
    Sun,
    TrendingUp,
    X,
} from 'lucide-react';
import { authApi } from '@/api/auth.api';
import { messagesApi } from '@/api/messages.api';
import { queryKeys } from '@/api/queryKeys';
import { AiChatWidget } from '@/components/common/AiChatWidget/AiChatWidget';
import { LanguageSwitcher } from '@/components/common/LanguageSwitcher';
import { Logo } from '@/components/common/Logo';
import { APP_ROUTES } from '@/config/routes';
import { useNotificationsHub } from '@/hooks/useNotificationsHub';
import { useAuthStore } from '@/store/auth.store';
import { useThemeStore } from '@/store/theme.store';
import { cn } from '@/utils/cn';

export function InstructorLayout() {
    const { t } = useTranslation('instructor');
    useNotificationsHub();
    const navigate = useNavigate();
    const { logout } = useAuthStore();
    const queryClient = useQueryClient();
    const { theme, toggleTheme } = useThemeStore();
    const location = useLocation();
    const [mobileOpen, setMobileOpen] = useState(false);
    const [prevPathname, setPrevPathname] = useState(location.pathname);

    if (location.pathname !== prevPathname) {
        setPrevPathname(location.pathname);
        setMobileOpen(false);
    }

    const { data: unreadData } = useQuery({
        queryKey: queryKeys.messages.unreadCount(),
        queryFn: messagesApi.getUnreadCount,
        staleTime: Infinity,
    });
    const unreadCount = unreadData?.totalUnread ?? 0;

    const navItems = [
        {
            to: APP_ROUTES.instructor.dashboard,
            label: t('navDashboard'),
            icon: <LayoutDashboard size={16} />,
            end: true,
        },
        {
            to: APP_ROUTES.instructor.courses,
            label: t('navMyCourses'),
            icon: <BookOpen size={16} />,
            end: true,
        },
        {
            to: APP_ROUTES.instructor.newCourse,
            label: t('navNewCourse'),
            icon: <PlusCircle size={16} />,
        },
        {
            to: APP_ROUTES.student.messages,
            label: t('navMessages'),
            icon: <MessageSquare size={16} />,
        },
        {
            to: APP_ROUTES.instructor.earnings,
            label: t('navEarnings'),
            icon: <TrendingUp size={16} />,
        },
    ];

    function handleSignOut() {
        authApi.logout().catch(() => {});
        logout();
        queryClient.clear();
        navigate(APP_ROUTES.public.login);
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
                        to={APP_ROUTES.public.home}
                        className="flex items-center gap-2 font-heading font-bold text-foreground"
                    >
                        <div className="grid size-8 place-items-center rounded-lg bg-primary text-primary-foreground shadow-sm">
                            <Logo className="size-5" />
                        </div>
                        <span className="tracking-tight">Learnix</span>
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
                        'fixed inset-0 top-14 z-40 flex flex-col border-r border-border bg-card transition-transform duration-200 md:static md:top-0 md:translate-x-0',
                        mobileOpen ? 'translate-x-0' : '-translate-x-full',
                    )}
                >
                    <div className="hidden items-center gap-2 px-4 py-5 md:flex">
                        <Link
                            to={APP_ROUTES.public.home}
                            className="flex items-center gap-2.5 font-heading font-bold text-foreground transition-opacity hover:opacity-90"
                        >
                            <div className="grid size-8 place-items-center rounded-lg bg-primary text-primary-foreground shadow-sm">
                                <Logo className="size-6" />
                            </div>
                            <span className="tracking-tight">Learnix</span>
                        </Link>
                    </div>

                    <div className="flex-1 px-3 py-2">
                        <p className="mb-2 px-2 text-xs uppercase tracking-wider text-muted-foreground">
                            Instructor
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
                                                ? 'bg-primary/10 font-medium text-primary'
                                                : 'text-foreground hover:bg-secondary',
                                        )
                                    }
                                >
                                    {item.icon}
                                    {item.label}
                                    {item.to === APP_ROUTES.student.messages && unreadCount > 0 && (
                                        <span className="ml-auto flex h-4 min-w-4 items-center justify-center rounded-full bg-destructive px-1 text-[10px] font-bold text-destructive-foreground">
                                            {unreadCount > 99 ? '99+' : unreadCount}
                                        </span>
                                    )}
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
                                to={APP_ROUTES.public.home}
                                className="flex items-center gap-2.5 rounded-lg px-3 py-2 text-foreground transition-colors hover:bg-secondary"
                            >
                                <ArrowLeft size={16} />
                                {t('navBackToCatalog')}
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
                                className="grid size-9 place-items-center rounded-lg text-muted-foreground transition-colors hover:bg-secondary hover:text-foreground"
                            >
                                {theme === 'dark' ? <Sun size={16} /> : <Moon size={16} />}
                            </button>
                        </div>
                    </div>
                </aside>

                {/* Main content */}
                <main className="h-full overflow-y-auto">
                    <Outlet />
                </main>
            </div>
            <AiChatWidget />
        </>
    );
}
