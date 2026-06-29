import type { ReactNode } from 'react';
import { useState } from 'react';
import { Helmet } from 'react-helmet-async';
import { Link, NavLink, Outlet, useLocation } from 'react-router-dom';
import { ArrowLeft, LogOut, Menu, Moon, Sun, User, X } from 'lucide-react';
import { LanguageSwitcher } from '@/components/common/ui/LanguageSwitcher';
import { APP_ROUTES } from '@/routes/paths';
import { useThemeStore } from '@/store/theme.store';
import { cn } from '@/utils/cn';

export interface DashboardNavItem {
    to: string;
    label: string;
    icon: ReactNode;
    end?: boolean;
    badge?: ReactNode;
}

export interface DashboardLayoutProps {
    roleLabel: string;
    themeColor?: 'primary' | 'destructive';
    logoNode: ReactNode;
    logoText: string;
    navItems: DashboardNavItem[];
    profileLabel?: string;
    backToLabel: string;
    signOutLabel: string;
    onSignOut: () => void;
    children?: ReactNode;
}

export function DashboardLayout({
    roleLabel,
    themeColor = 'primary',
    logoNode,
    logoText,
    navItems,
    profileLabel,
    backToLabel,
    signOutLabel,
    onSignOut,
    children,
}: DashboardLayoutProps) {
    const { theme, toggleTheme } = useThemeStore();
    const location = useLocation();
    const [mobileOpen, setMobileOpen] = useState(false);

    const [prevPathname, setPrevPathname] = useState(location.pathname);

    if (location.pathname !== prevPathname) {
        setPrevPathname(location.pathname);
        setMobileOpen(false);
    }

    const activeBgClass = themeColor === 'destructive' ? 'bg-destructive/10' : 'bg-primary/10';
    const activeTextClass = themeColor === 'destructive' ? 'text-destructive' : 'text-primary';

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
                        {logoNode}
                        <span className="tracking-tight">{logoText}</span>
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
                        'overflow-y-auto',
                    )}
                >
                    <div className="hidden items-center gap-2 px-4 py-5 md:flex">
                        <Link
                            to={APP_ROUTES.public.home}
                            className="flex items-center gap-2.5 font-heading font-bold text-foreground transition-opacity hover:opacity-90"
                        >
                            {logoNode}
                            <span className="tracking-tight">{logoText}</span>
                        </Link>
                    </div>

                    <div className="flex-1 px-3 py-2">
                        <p className="mb-2 px-2 text-xs uppercase tracking-wider text-muted-foreground">
                            {roleLabel}
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
                                                ? `${activeBgClass} font-medium ${activeTextClass}`
                                                : 'text-foreground hover:bg-secondary',
                                        )
                                    }
                                >
                                    {item.icon}
                                    {item.label}
                                    {item.badge}
                                </NavLink>
                            ))}
                        </nav>
                    </div>

                    <div className="border-t border-border px-3 py-4">
                        <p className="mb-2 px-2 text-xs uppercase tracking-wider text-muted-foreground">
                            Account
                        </p>
                        <nav className="space-y-1 text-sm">
                            {profileLabel && (
                                <Link
                                    to={APP_ROUTES.student.profile}
                                    className="flex items-center gap-2.5 rounded-lg px-3 py-2 text-foreground transition-colors hover:bg-secondary"
                                >
                                    <User size={16} />
                                    {profileLabel}
                                </Link>
                            )}
                            <Link
                                to={APP_ROUTES.public.home}
                                className="flex items-center gap-2.5 rounded-lg px-3 py-2 text-foreground transition-colors hover:bg-secondary"
                            >
                                <ArrowLeft size={16} />
                                {backToLabel}
                            </Link>
                            <button
                                onClick={onSignOut}
                                className="flex w-full items-center gap-2.5 rounded-lg px-3 py-2 text-muted-foreground transition-colors hover:bg-secondary hover:text-foreground"
                            >
                                <LogOut size={16} />
                                {signOutLabel}
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
            {children}
        </>
    );
}
