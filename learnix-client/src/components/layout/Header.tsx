import { useRef, useState, useEffect } from 'react';
import { Link, NavLink, useNavigate } from 'react-router-dom';
import { Sun, Moon, LogOut, User, BookOpen } from 'lucide-react';
import { useQueryClient } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { cn } from '@/utils/cn';
import { useAuthStore } from '@/store/auth.store';
import { useThemeStore } from '@/store/theme.store';
import { authApi } from '@/api/auth.api';
import { NotificationBell } from './NotificationBell';
import { MessagesButton } from './MessagesButton';
import { WishlistButton } from './WishlistButton';
import { LanguageSwitcher } from '@/components/common/LanguageSwitcher';
import { Logo } from '@/components/common/Logo';

function UserMenu({
    fullName,
    email,
    avatarUrl,
}: {
    fullName: string;
    email: string;
    avatarUrl: string | null;
}) {
    const { t } = useTranslation('header');
    const [open, setOpen] = useState(false);
    const ref = useRef<HTMLDivElement>(null);
    const navigate = useNavigate();
    const { logout } = useAuthStore();
    const queryClient = useQueryClient();

    const initials = fullName
        .split(' ')
        .map((n) => n[0])
        .slice(0, 2)
        .join('')
        .toUpperCase();

    useEffect(() => {
        function handleClickOutside(e: MouseEvent) {
            if (ref.current && !ref.current.contains(e.target as Node)) {
                setOpen(false);
            }
        }
        document.addEventListener('mousedown', handleClickOutside);
        return () => document.removeEventListener('mousedown', handleClickOutside);
    }, []);

    function handleSignOut() {
        authApi.logout().catch(() => {});
        logout();
        queryClient.clear();
        navigate('/login');
    }

    return (
        <div ref={ref} className="relative">
            <button
                type="button"
                onClick={() => setOpen((v) => !v)}
                className="flex items-center transition-opacity hover:opacity-80"
            >
                <div className="flex h-8 w-8 shrink-0 items-center justify-center overflow-hidden rounded-full bg-primary/15 text-xs font-semibold text-primary">
                    {avatarUrl ? (
                        <img
                            src={avatarUrl}
                            alt={fullName}
                            className="h-full w-full object-cover"
                        />
                    ) : (
                        initials
                    )}
                </div>
            </button>

            {open && (
                <div className="absolute right-0 top-full z-50 mt-2 w-56 overflow-hidden rounded-xl border border-border bg-card shadow-lg">
                    <div className="flex items-center gap-3 px-4 py-3">
                        <div className="flex h-10 w-10 shrink-0 items-center justify-center overflow-hidden rounded-full bg-primary/15 text-sm font-semibold text-primary">
                            {avatarUrl ? (
                                <img
                                    src={avatarUrl}
                                    alt={fullName}
                                    className="h-full w-full object-cover"
                                />
                            ) : (
                                initials
                            )}
                        </div>
                        <div className="min-w-0">
                            <p className="truncate text-sm font-medium text-foreground">
                                {fullName}
                            </p>
                            <p className="truncate text-xs text-muted-foreground">{email}</p>
                        </div>
                    </div>
                    <div className="border-t border-border" />
                    <Link
                        to="/profile"
                        onClick={() => setOpen(false)}
                        className="flex items-center gap-2.5 px-4 py-2.5 text-sm text-foreground transition-colors hover:bg-secondary"
                    >
                        <User size={14} className="text-muted-foreground" />
                        {t('menuProfile')}
                    </Link>
                    <Link
                        to="/my-learning"
                        onClick={() => setOpen(false)}
                        className="flex items-center gap-2.5 px-4 py-2.5 text-sm text-foreground transition-colors hover:bg-secondary"
                    >
                        <BookOpen size={14} className="text-muted-foreground" />
                        {t('menuMyLearning')}
                    </Link>
                    <div className="my-1 border-t border-border" />
                    <button
                        type="button"
                        onClick={handleSignOut}
                        className="flex w-full items-center gap-2.5 px-4 py-2.5 text-sm text-muted-foreground transition-colors hover:bg-secondary hover:text-foreground"
                    >
                        <LogOut size={14} />
                        {t('menuSignOut')}
                    </button>
                </div>
            )}
        </div>
    );
}

export function Header() {
    const { t } = useTranslation('header');
    const user = useAuthStore((s) => s.user);
    const { theme, toggleTheme } = useThemeStore();

    const navItems = [
        { to: '/courses', label: t('navCourses') },
        ...(user?.role === 'Instructor'
            ? [{ to: '/instructor', label: t('navInstructorPanel') }]
            : []),
        ...(user?.role === 'Admin' ? [{ to: '/admin', label: t('navAdminPanel') }] : []),
    ];

    return (
        <header className="sticky top-0 z-40 border-b border-border bg-background/90 backdrop-blur">
            <div className="mx-auto flex h-16 max-w-7xl items-center justify-between px-6">
                <div className="flex items-center gap-10">
                    <Link
                        to="/"
                        className="flex items-center gap-2.5 transition-opacity hover:opacity-90"
                    >
                        <div className="grid h-8 w-8 place-items-center rounded-lg bg-primary text-primary-foreground shadow-sm">
                            <Logo className="h-6 w-6" />
                        </div>
                        <span className="font-heading text-lg font-bold tracking-tight">
                            Learnix
                        </span>
                    </Link>
                    <nav className="hidden items-center gap-7 text-sm md:flex">
                        {navItems.map((item) => (
                            <NavLink
                                key={item.to}
                                to={item.to}
                                className={({ isActive }) =>
                                    cn(
                                        'transition-colors hover:text-primary',
                                        isActive ? 'text-foreground' : 'text-muted-foreground',
                                    )
                                }
                            >
                                {item.label}
                            </NavLink>
                        ))}
                    </nav>
                </div>
                <div className="flex items-center gap-3">
                    <LanguageSwitcher />
                    <button
                        type="button"
                        onClick={toggleTheme}
                        aria-label="Toggle theme"
                        className="grid h-9 w-9 place-items-center rounded-lg text-muted-foreground transition-colors hover:bg-secondary hover:text-foreground"
                    >
                        {theme === 'dark' ? (
                            <Sun className="h-4 w-4" />
                        ) : (
                            <Moon className="h-4 w-4" />
                        )}
                    </button>
                    {user ? (
                        <>
                            <MessagesButton />
                            <NotificationBell />
                            <WishlistButton />
                            <UserMenu
                                fullName={user.fullName}
                                email={user.email}
                                avatarUrl={user.avatarUrl}
                            />
                        </>
                    ) : (
                        <>
                            <Link
                                to="/login"
                                className="hidden text-sm text-foreground hover:text-primary md:block"
                            >
                                {t('login')}
                            </Link>
                            <Link
                                to="/register"
                                className="rounded-lg bg-primary px-4 py-2 text-sm font-medium text-primary-foreground transition-colors hover:bg-primary/90"
                            >
                                {t('getStarted')}
                            </Link>
                        </>
                    )}
                </div>
            </div>
        </header>
    );
}
