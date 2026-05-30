import { Link, NavLink } from 'react-router-dom';
import { Sun, Moon } from 'lucide-react';
import { cn } from '@/utils/cn';
import { useAuthStore } from '@/store/auth.store';
import { useThemeStore } from '@/store/theme.store';
import { NotificationBell } from './NotificationBell';
import { WishlistButton } from './WishlistButton';
import { HEADER } from '@/const/localization/header';

function UserAvatar({ fullName, avatarUrl }: { fullName: string; avatarUrl: string | null }) {
    const initials = fullName
        .split(' ')
        .map((n) => n[0])
        .slice(0, 2)
        .join('')
        .toUpperCase();

    return (
        <Link
            to="/profile"
            className="flex items-center gap-2.5 transition-opacity hover:opacity-80"
        >
            <div className="flex h-8 w-8 shrink-0 items-center justify-center overflow-hidden rounded-full bg-primary/15 text-xs font-semibold text-primary">
                {avatarUrl ? (
                    <img src={avatarUrl} alt={fullName} className="h-full w-full object-cover" />
                ) : (
                    initials
                )}
            </div>
            <span className="hidden text-sm font-medium text-foreground md:block">{fullName}</span>
        </Link>
    );
}

export function Header() {
    const user = useAuthStore((s) => s.user);
    const { theme, toggleTheme } = useThemeStore();

    const navItems = [
        { to: '/courses', label: HEADER.NAV_COURSES },
        ...(user?.role === 'Instructor'
            ? [{ to: '/instructor', label: HEADER.NAV_INSTRUCTOR_PANEL }]
            : []),
        ...(user?.role === 'Admin' ? [{ to: '/admin', label: HEADER.NAV_ADMIN_PANEL }] : []),
    ];

    return (
        <header className="sticky top-0 z-40 border-b border-border bg-background/90 backdrop-blur">
            <div className="mx-auto flex h-16 max-w-7xl items-center justify-between px-6">
                <div className="flex items-center gap-10">
                    <Link to="/" className="flex items-center gap-2">
                        <div className="grid h-8 w-8 place-items-center rounded-lg bg-primary font-heading font-bold text-primary-foreground">
                            L
                        </div>
                        <span className="font-heading text-lg font-bold">Learnix</span>
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
                            <NotificationBell />
                            <WishlistButton />
                            <NavLink
                                to="/my-learning"
                                className={({ isActive }) =>
                                    cn(
                                        'hidden text-sm transition-colors hover:text-primary md:block',
                                        isActive ? 'text-foreground' : 'text-muted-foreground',
                                    )
                                }
                            >
                                {HEADER.NAV_MY_LEARNING}
                            </NavLink>
                            <UserAvatar fullName={user.fullName} avatarUrl={user.avatarUrl} />
                        </>
                    ) : (
                        <>
                            <Link
                                to="/login"
                                className="hidden text-sm text-foreground hover:text-primary md:block"
                            >
                                {HEADER.LOGIN}
                            </Link>
                            <Link
                                to="/register"
                                className="rounded-lg bg-primary px-4 py-2 text-sm font-medium text-primary-foreground transition-colors hover:bg-primary/90"
                            >
                                {HEADER.GET_STARTED}
                            </Link>
                        </>
                    )}
                </div>
            </div>
        </header>
    );
}
