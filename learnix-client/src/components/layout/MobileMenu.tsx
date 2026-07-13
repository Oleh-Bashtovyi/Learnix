import { type ReactNode, useEffect, useState } from 'react';
import { createPortal } from 'react-dom';
import { useTranslation } from 'react-i18next';
import { Link, NavLink, useLocation } from 'react-router-dom';
import { BookOpen, CircleHelp, Heart, LogOut, Menu, MessageSquare, User, X } from 'lucide-react';
import { BrandLogo } from '@/components/common/ui/BrandLogo';
import { LanguageSwitcher } from '@/components/common/ui/LanguageSwitcher';
import { ThemeSwitcher } from '@/components/common/ui/ThemeSwitcher';
import { useLogout } from '@/hooks/auth/useLogout';
import { APP_ROUTES } from '@/routes/paths';
import { useAuthStore } from '@/store/auth.store';
import { cn } from '@/utils/cn';

interface NavItem {
    to: string;
    label: string;
    icon?: ReactNode;
}

interface MobileMenuProps {
    navItems: NavItem[];
}

export function MobileMenu({ navItems }: MobileMenuProps) {
    const { t } = useTranslation('header');
    const [isOpen, setIsOpen] = useState(false);
    const user = useAuthStore((s) => s.user);
    const location = useLocation();
    const signOut = useLogout();

    const [prevPathname, setPrevPathname] = useState(location.pathname);

    // Close menu when route changes
    if (location.pathname !== prevPathname) {
        setPrevPathname(location.pathname);
        if (isOpen) setIsOpen(false);
    }

    // Prevent scrolling when menu is open, and close on resize
    useEffect(() => {
        if (isOpen) {
            document.body.style.overflow = 'hidden';

            const handleResize = () => {
                if (window.innerWidth >= 640) {
                    setIsOpen(false);
                }
            };
            window.addEventListener('resize', handleResize);

            return () => {
                document.body.style.overflow = '';
                window.removeEventListener('resize', handleResize);
            };
        } else {
            document.body.style.overflow = '';
        }
    }, [isOpen]);

    return (
        <>
            <button
                type="button"
                onClick={() => setIsOpen(true)}
                className="flex size-10 items-center justify-center rounded-lg text-muted-foreground hover:bg-secondary hover:text-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary focus-visible:ring-offset-2"
                aria-label="Open menu"
            >
                <Menu size={24} />
            </button>

            {/* Backdrop & Drawer rendered via Portal to escape Header's backdrop-filter containing block */}
            {typeof document !== 'undefined' &&
                createPortal(
                    <>
                        {/* Backdrop — a button, so dismissing the menu is not mouse-only */}
                        <button
                            type="button"
                            aria-label={t('common:actions.close')}
                            className={cn(
                                'fixed inset-0 z-50 bg-background/80 backdrop-blur-sm transition-opacity duration-300',
                                isOpen ? 'opacity-100' : 'pointer-events-none opacity-0',
                            )}
                            onClick={() => setIsOpen(false)}
                        />

                        {/* Drawer */}
                        <div
                            className={cn(
                                'fixed inset-y-0 left-0 z-50 flex w-full max-w-xs flex-col bg-background shadow-2xl transition-transform duration-300 ease-in-out sm:max-w-sm',
                                isOpen ? 'translate-x-0' : '-translate-x-full',
                            )}
                        >
                            <div className="flex h-16 shrink-0 items-center justify-between border-b border-border px-4 sm:px-6">
                                <BrandLogo onClick={() => setIsOpen(false)} />
                                <button
                                    type="button"
                                    onClick={() => setIsOpen(false)}
                                    className="flex size-10 items-center justify-center rounded-lg text-muted-foreground hover:bg-secondary hover:text-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary focus-visible:ring-offset-2"
                                >
                                    <X size={24} />
                                </button>
                            </div>

                            {user && (
                                <div className="shrink-0 border-b border-border p-4 sm:px-6">
                                    <Link
                                        to={APP_ROUTES.student.profile}
                                        onClick={() => setIsOpen(false)}
                                        className="flex items-center gap-3 rounded-xl border border-border/50 bg-secondary/30 p-4 transition-colors hover:bg-secondary/50 active:scale-[0.98]"
                                    >
                                        <div className="flex size-12 shrink-0 items-center justify-center rounded-full bg-primary text-lg font-bold text-primary-foreground shadow-sm">
                                            {user.fullName.charAt(0).toUpperCase()}
                                        </div>
                                        <div className="flex min-w-0 flex-col">
                                            <span className="truncate text-base font-bold text-foreground">
                                                {user.fullName}
                                            </span>
                                            <span className="truncate text-sm text-muted-foreground">
                                                {user.email}
                                            </span>
                                        </div>
                                    </Link>
                                </div>
                            )}

                            <div className="flex flex-1 flex-col overflow-y-auto p-4 sm:px-6">
                                <nav className="flex flex-col gap-1">
                                    {navItems.map((item) => (
                                        <NavLink
                                            key={item.to}
                                            to={item.to}
                                            onClick={() => setIsOpen(false)}
                                            className={({ isActive }) =>
                                                cn(
                                                    'flex items-center gap-3 rounded-lg px-4 py-3 text-base font-medium transition-colors',
                                                    isActive
                                                        ? 'bg-primary/10 text-primary'
                                                        : 'text-muted-foreground hover:bg-secondary/50 hover:text-foreground',
                                                )
                                            }
                                        >
                                            {item.icon}
                                            {item.label}
                                        </NavLink>
                                    ))}
                                </nav>

                                {user && (
                                    <>
                                        <div className="my-4 border-t border-border" />
                                        <div className="flex flex-col gap-1">
                                            <NavLink
                                                to={APP_ROUTES.student.profile}
                                                onClick={() => setIsOpen(false)}
                                                className={({ isActive }) =>
                                                    cn(
                                                        'flex items-center gap-3 rounded-lg px-4 py-3 text-base font-medium transition-colors',
                                                        isActive
                                                            ? 'bg-primary/10 text-primary'
                                                            : 'text-muted-foreground hover:bg-secondary/50 hover:text-foreground',
                                                    )
                                                }
                                            >
                                                <User size={20} />
                                                {t('menuProfile')}
                                            </NavLink>
                                            <NavLink
                                                to={APP_ROUTES.student.myLearning}
                                                onClick={() => setIsOpen(false)}
                                                className={({ isActive }) =>
                                                    cn(
                                                        'flex items-center gap-3 rounded-lg px-4 py-3 text-base font-medium transition-colors',
                                                        isActive
                                                            ? 'bg-primary/10 text-primary'
                                                            : 'text-muted-foreground hover:bg-secondary/50 hover:text-foreground',
                                                    )
                                                }
                                            >
                                                <BookOpen size={20} />
                                                {t('common:navigation.myLearning')}
                                            </NavLink>
                                            <NavLink
                                                to={APP_ROUTES.student.wishlist}
                                                onClick={() => setIsOpen(false)}
                                                className={({ isActive }) =>
                                                    cn(
                                                        'flex items-center gap-3 rounded-lg px-4 py-3 text-base font-medium transition-colors',
                                                        isActive
                                                            ? 'bg-primary/10 text-primary'
                                                            : 'text-muted-foreground hover:bg-secondary/50 hover:text-foreground',
                                                    )
                                                }
                                            >
                                                <Heart size={20} />
                                                {t('common:navigation.wishlist')}
                                            </NavLink>
                                            <NavLink
                                                to={APP_ROUTES.student.messages}
                                                onClick={() => setIsOpen(false)}
                                                className={({ isActive }) =>
                                                    cn(
                                                        'flex items-center gap-3 rounded-lg px-4 py-3 text-base font-medium transition-colors',
                                                        isActive
                                                            ? 'bg-primary/10 text-primary'
                                                            : 'text-muted-foreground hover:bg-secondary/50 hover:text-foreground',
                                                    )
                                                }
                                            >
                                                <MessageSquare size={20} />
                                                {t('common:navigation.messages')}
                                            </NavLink>
                                            <NavLink
                                                to={APP_ROUTES.public.faq}
                                                onClick={() => setIsOpen(false)}
                                                className={({ isActive }) =>
                                                    cn(
                                                        'flex items-center gap-3 rounded-lg px-4 py-3 text-base font-medium transition-colors',
                                                        isActive
                                                            ? 'bg-primary/10 text-primary'
                                                            : 'text-muted-foreground hover:bg-secondary/50 hover:text-foreground',
                                                    )
                                                }
                                            >
                                                <CircleHelp size={20} />
                                                {t('menuHelp')}
                                            </NavLink>
                                        </div>
                                    </>
                                )}
                            </div>

                            <div className="shrink-0 border-t border-border p-4 sm:px-6">
                                <div className="mb-1 space-y-1">
                                    <ThemeSwitcher variant="mobileMenu" />
                                    <LanguageSwitcher variant="mobileMenu" />
                                </div>

                                {!user ? (
                                    <div className="flex flex-col gap-2">
                                        <Link
                                            to={APP_ROUTES.public.login}
                                            state={{ from: location }}
                                            onClick={() => setIsOpen(false)}
                                            className="flex h-11 items-center justify-center rounded-lg border border-border px-4 font-medium transition-colors hover:bg-secondary"
                                        >
                                            {t('common:actions.logIn')}
                                        </Link>
                                        <Link
                                            to={APP_ROUTES.public.register}
                                            state={{ from: location }}
                                            onClick={() => setIsOpen(false)}
                                            className="flex h-11 items-center justify-center rounded-lg bg-primary px-4 font-medium text-primary-foreground transition-colors hover:bg-primary/90"
                                        >
                                            {t('getStarted')}
                                        </Link>
                                    </div>
                                ) : (
                                    <button
                                        type="button"
                                        onClick={signOut}
                                        className="flex w-full items-center gap-3 rounded-lg px-4 py-3 text-base font-medium text-destructive transition-colors hover:bg-destructive/10"
                                    >
                                        <LogOut size={20} />
                                        {t('common:actions.signOut')}
                                    </button>
                                )}
                            </div>
                        </div>
                    </>,
                    document.body,
                )}
        </>
    );
}
