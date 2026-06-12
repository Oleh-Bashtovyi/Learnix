import { NavLink, Outlet } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { cn } from '@/utils/cn';

export function StudentDashboardLayout() {
    const { t } = useTranslation('myLearning');

    const tabs = [
        { to: '/my-learning', label: t('tabAllCourses') },
        { to: '/wishlist', label: t('tabWishlist') },
        { to: '/certificates', label: t('tabCertificates') },
    ];

    return (
        <div className="flex min-h-full flex-col bg-background">
            <div className="bg-zinc-900 text-zinc-50 dark:bg-zinc-950">
                <div className="mx-auto max-w-7xl px-4 pt-12 sm:px-6">
                    <h1 className="font-heading text-3xl font-bold md:text-4xl">{t('title')}</h1>
                    <nav className="mt-8 flex gap-6 overflow-x-auto text-sm font-medium">
                        {tabs.map((tab) => (
                            <NavLink
                                key={tab.to}
                                to={tab.to}
                                end={tab.to === '/my-learning'}
                                className={({ isActive }) =>
                                    cn(
                                        'whitespace-nowrap border-b-2 py-3 transition-colors hover:text-zinc-50',
                                        isActive
                                            ? 'border-zinc-50 text-zinc-50'
                                            : 'border-transparent text-zinc-400',
                                    )
                                }
                            >
                                {tab.label}
                            </NavLink>
                        ))}
                    </nav>
                </div>
            </div>
            <div className="flex-1">
                <Outlet />
            </div>
        </div>
    );
}
