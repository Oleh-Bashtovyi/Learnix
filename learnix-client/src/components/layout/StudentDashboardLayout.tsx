import { useTranslation } from 'react-i18next';
import { NavLink, Outlet } from 'react-router-dom';
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
            <div className="border-b border-zinc-800 bg-zinc-900 text-zinc-50 dark:bg-zinc-950">
                <div className="mx-auto flex max-w-7xl flex-col gap-4 px-4 py-6 sm:px-6 md:flex-row md:items-center md:gap-8 md:py-4">
                    <h1 className="font-heading text-2xl font-bold md:text-3xl">{t('title')}</h1>
                    <nav className="scrollbar-hide -mx-4 flex gap-2 overflow-x-auto px-4 pb-2 text-sm font-medium sm:mx-0 sm:px-0 sm:pb-0 md:ml-4">
                        {tabs.map((tab) => (
                            <NavLink
                                key={tab.to}
                                to={tab.to}
                                end={tab.to === '/my-learning'}
                                className={({ isActive }) =>
                                    cn(
                                        'whitespace-nowrap rounded-lg px-4 py-2 transition-colors',
                                        isActive
                                            ? 'bg-zinc-800 text-zinc-50'
                                            : 'text-zinc-400 hover:bg-zinc-800/50 hover:text-zinc-50',
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
