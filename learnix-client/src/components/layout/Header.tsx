import { useEffect, useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link, NavLink, useLocation, useNavigate } from 'react-router-dom';
import { useQueryClient } from '@tanstack/react-query';
import { BookOpen, Compass, GraduationCap, LogOut, Moon, Shield, Sun, User } from 'lucide-react';
import { authApi } from '@/api/auth.api';
import { LanguageSwitcher } from '@/components/common/ui/LanguageSwitcher';
import { Logo } from '@/components/common/ui/Logo';
import { APP_ROUTES } from '@/routes/paths';
import { useAuthStore } from '@/store/auth.store';
import { useThemeStore } from '@/store/theme.store';
import { cn } from '@/utils/cn';
import { MessagesButton } from './MessagesButton';
import { MobileMenu } from './MobileMenu';
import { NotificationBell } from './NotificationBell';
import { WishlistButton } from './WishlistButton';

type UserMenuProps = {
    fullName: string;
    email: string;
    avatarUrl: string | null;
};

function UserMenu({ fullName, email, avatarUrl }: UserMenuProps) {
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

    /**
     * Related ADRs:
     * - ADR-FRONT-AUTH-004: Explicit Logout & State Clearing
     */
    function handleSignOut() {
        authApi.logout().catch(() => {});
        logout();
        queryClient.clear();
        navigate(APP_ROUTES.public.login);
    }

    const closeTimeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null);

    function handleMouseEnter() {
        if (closeTimeoutRef.current) {
            clearTimeout(closeTimeoutRef.current);
        }
        setOpen(true);
    }

    function handleMouseLeave() {
        closeTimeoutRef.current = setTimeout(() => {
            setOpen(false);
        }, 300); // grace period for diagonal mouse movement
    }

    return (
        <div
            ref={ref}
            className="relative"
            onMouseEnter={handleMouseEnter}
            onMouseLeave={handleMouseLeave}
        >
            <button
                type="button"
                onClick={() => setOpen(true)}
                className="flex items-center transition-opacity hover:opacity-80"
            >
                <div className="flex size-8 shrink-0 items-center justify-center overflow-hidden rounded-full bg-primary/15 text-xs font-semibold text-primary">
                    {avatarUrl ? (
                        <img src={avatarUrl} alt={fullName} className="size-full object-cover" />
                    ) : (
                        initials
                    )}
                </div>
            </button>

            {open && (
                <div className="absolute right-0 top-full z-50 pt-2">
                    <div className="w-64 overflow-hidden rounded-xl border border-border bg-card shadow-lg">
                        <div className="flex items-center gap-3 px-4 py-3">
                            <div className="flex size-10 shrink-0 items-center justify-center overflow-hidden rounded-full bg-primary/15 text-sm font-semibold text-primary">
                                {avatarUrl ? (
                                    <img
                                        src={avatarUrl}
                                        alt={fullName}
                                        className="size-full object-cover"
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
                            to={APP_ROUTES.student.profile}
                            onClick={() => setOpen(false)}
                            className="flex items-center gap-2.5 px-4 py-2.5 text-sm text-foreground transition-colors hover:bg-secondary"
                        >
                            <User size={14} className="text-muted-foreground" />
                            {t('menuProfile')}
                        </Link>
                        <Link
                            to={APP_ROUTES.student.myLearning}
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
                </div>
            )}
        </div>
    );
}

export function Header() {
    const { t } = useTranslation('header');
    const user = useAuthStore((s) => s.user);
    const { theme, toggleTheme } = useThemeStore();
    const location = useLocation();

    const navItems = [
        { to: APP_ROUTES.public.courses, label: t('navCourses'), icon: <Compass size={20} /> },
        ...(user?.roles.includes('Instructor')
            ? [
                  {
                      to: APP_ROUTES.instructor.dashboard,
                      label: t('navInstructorPanel'),
                      icon: <GraduationCap size={20} />,
                  },
              ]
            : []),
        ...(user?.roles.includes('Admin')
            ? [
                  {
                      to: APP_ROUTES.admin.dashboard,
                      label: t('navAdminPanel'),
                      icon: <Shield size={20} />,
                  },
              ]
            : []),
    ];

    return (
        <header className="sticky top-0 z-40 border-b border-border bg-card/95 shadow-sm backdrop-blur supports-[backdrop-filter]:bg-card/60">
            <div className="mx-auto flex h-16 max-w-7xl items-center justify-between px-4 sm:px-6">
                <div className="flex items-center sm:gap-10">
                    <div className="mr-2 sm:hidden">
                        <MobileMenu navItems={navItems} />
                    </div>
                    <Link
                        to={APP_ROUTES.public.home}
                        className="flex items-center gap-2.5 transition-opacity hover:opacity-90"
                    >
                        <div className="grid size-8 place-items-center rounded-lg bg-primary text-primary-foreground shadow-sm">
                            <Logo className="size-6" />
                        </div>
                        <span className="font-heading text-lg font-bold tracking-tight">
                            Learnix
                        </span>
                    </Link>
                    <nav className="hidden items-center gap-7 text-sm sm:flex">
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
                <div className="flex items-center gap-2 sm:gap-3">
                    <div className="hidden sm:block">
                        <LanguageSwitcher />
                    </div>
                    <button
                        type="button"
                        onClick={toggleTheme}
                        aria-label="Toggle theme"
                        className="hidden size-9 place-items-center rounded-lg text-muted-foreground transition-colors hover:bg-secondary hover:text-foreground sm:grid"
                    >
                        {theme === 'dark' ? (
                            <Sun className="size-4" />
                        ) : (
                            <Moon className="size-4" />
                        )}
                    </button>
                    {user ? (
                        <>
                            <div className="hidden sm:block">
                                <MessagesButton />
                            </div>
                            <NotificationBell />
                            <div className="hidden sm:block">
                                <WishlistButton />
                            </div>
                            <UserMenu
                                fullName={user.fullName}
                                email={user.email}
                                avatarUrl={user.avatarUrl}
                            />
                        </>
                    ) : (
                        <>
                            <Link
                                to={APP_ROUTES.public.login}
                                state={{ from: location }}
                                className="hidden text-sm text-foreground hover:text-primary sm:block"
                            >
                                {t('login')}
                            </Link>
                            <Link
                                to={APP_ROUTES.public.register}
                                state={{ from: location }}
                                className="rounded-lg bg-primary px-3 py-1.5 text-xs font-medium text-primary-foreground transition-colors hover:bg-primary/90 sm:px-4 sm:py-2 sm:text-sm"
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
