import { useEffect, useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link, NavLink, useLocation } from 'react-router-dom';
import { BookOpen, CircleHelp, Compass, GraduationCap, LogOut, Shield, User } from 'lucide-react';
import { BrandLogo } from '@/components/common/elements/BrandLogo';
import { LanguageSwitcher } from '@/components/common/elements/LanguageSwitcher';
import { ThemeSwitcher } from '@/components/common/elements/ThemeSwitcher';
import { HEADER_ICON_SIZE } from '@/const/ui.constants';
import { UserRole } from '@/enums/user.enums';
import { useLogout } from '@/hooks/auth/useLogout';
import { APP_ROUTES } from '@/routes/paths';
import { useAuthStore } from '@/store/auth.store';
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
    const signOut = useLogout();

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

    const avatarContent = (
        <div className="flex size-8 shrink-0 items-center justify-center overflow-hidden rounded-full bg-primary/15 text-xs font-semibold text-primary">
            {avatarUrl ? (
                <img src={avatarUrl} alt={fullName} className="size-full object-cover" />
            ) : (
                initials
            )}
        </div>
    );

    return (
        <div
            ref={ref}
            className="relative"
            onMouseEnter={handleMouseEnter}
            onMouseLeave={handleMouseLeave}
        >
            <Link
                to={APP_ROUTES.student.profile}
                onClick={() => setOpen(false)}
                className="hidden items-center transition-opacity hover:opacity-80 sm:flex"
            >
                {avatarContent}
            </Link>

            <Link
                to={APP_ROUTES.student.profile}
                className="flex items-center transition-opacity hover:opacity-80 sm:hidden"
            >
                {avatarContent}
            </Link>

            {open && (
                <div className="absolute right-0 top-full z-50 hidden pt-2 sm:block">
                    <div className="w-64 overflow-hidden rounded-xl border border-border bg-popover text-popover-foreground shadow-2xl">
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
                            className="flex items-center gap-2.5 px-4 py-2.5 text-sm text-foreground transition-colors hover:bg-foreground/10"
                        >
                            <User
                                size={HEADER_ICON_SIZE.dropdownItem}
                                className="text-muted-foreground"
                            />
                            {t('menuProfile')}
                        </Link>
                        <Link
                            to={APP_ROUTES.student.myLearning}
                            onClick={() => setOpen(false)}
                            className="flex items-center gap-2.5 px-4 py-2.5 text-sm text-foreground transition-colors hover:bg-foreground/10"
                        >
                            <BookOpen
                                size={HEADER_ICON_SIZE.dropdownItem}
                                className="text-muted-foreground"
                            />
                            {t('common:navigation.myLearning')}
                        </Link>
                        {/* Signed-in users never see the landing page, where the other FAQ entry lives. */}
                        <Link
                            to={APP_ROUTES.public.faq}
                            onClick={() => setOpen(false)}
                            className="flex items-center gap-2.5 px-4 py-2.5 text-sm text-foreground transition-colors hover:bg-foreground/10"
                        >
                            <CircleHelp
                                size={HEADER_ICON_SIZE.dropdownItem}
                                className="text-muted-foreground"
                            />
                            {t('menuHelp')}
                        </Link>
                        <div className="my-1 border-t border-border" />
                        <button
                            type="button"
                            onClick={signOut}
                            className="flex w-full items-center gap-2.5 px-4 py-2.5 text-sm text-muted-foreground transition-colors hover:bg-foreground/10 hover:text-foreground"
                        >
                            <LogOut size={HEADER_ICON_SIZE.dropdownItem} />
                            {t('common:actions.signOut')}
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
    const location = useLocation();

    const navItems = [
        {
            to: APP_ROUTES.public.courses,
            label: t('common:navigation.courses'),
            icon: <Compass size={HEADER_ICON_SIZE.action} />,
        },
        ...(user?.roles.includes(UserRole.Instructor)
            ? [
                  {
                      to: APP_ROUTES.instructor.dashboard,
                      label: t('navInstructorPanel'),
                      icon: <GraduationCap size={HEADER_ICON_SIZE.action} />,
                  },
              ]
            : []),
        ...(user?.roles.includes(UserRole.Admin)
            ? [
                  {
                      to: APP_ROUTES.admin.dashboard,
                      label: t('navAdminPanel'),
                      icon: <Shield size={HEADER_ICON_SIZE.action} />,
                  },
              ]
            : []),
    ];

    // Desktop only: on mobile "My Learning" already has its own entry in the drawer's user
    // section, and the avatar dropdown that holds it on desktop only opens on hover.
    const desktopNavItems = user
        ? [
              navItems[0],
              {
                  to: APP_ROUTES.student.myLearning,
                  label: t('common:navigation.myLearning'),
                  icon: <BookOpen size={HEADER_ICON_SIZE.action} />,
              },
              ...navItems.slice(1),
          ]
        : navItems;

    return (
        <header className="sticky top-0 z-40 border-b border-border bg-card/95 shadow-sm backdrop-blur supports-[backdrop-filter]:bg-card/60">
            <div className="mx-auto flex h-16 max-w-7xl items-center justify-between px-4 sm:px-6">
                <div className="flex items-center sm:gap-10">
                    <div className="mr-2 sm:hidden">
                        <MobileMenu navItems={navItems} />
                    </div>
                    <BrandLogo />
                    <nav className="hidden items-center gap-7 text-sm sm:flex">
                        {desktopNavItems.map((item) => (
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
                    <ThemeSwitcher className="hidden sm:grid" />
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
                                {t('common:actions.logIn')}
                            </Link>
                            <Link
                                to={APP_ROUTES.public.register}
                                state={{ from: location }}
                                className="shrink-0 rounded-lg bg-primary px-4 py-2 text-sm font-medium text-primary-foreground transition-colors hover:bg-primary/90"
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
