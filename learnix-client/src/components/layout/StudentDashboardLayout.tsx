import { useTranslation } from 'react-i18next';
import { NavLink, Outlet } from 'react-router-dom';
import { APP_ROUTES } from '@/routes/paths';
import { cn } from '@/utils/cn';

export function StudentDashboardLayout() {
    const { t } = useTranslation('myLearning');

    const tabs = [
        { to: APP_ROUTES.student.myLearning, label: t('common:navigation.allCourses') },
        { to: APP_ROUTES.student.wishlist, label: t('common:navigation.wishlist') },
        { to: APP_ROUTES.student.certificates, label: t('common:navigation.certificates') },
        { to: APP_ROUTES.student.achievements, label: t('common:navigation.achievements') },
    ];

    return (
        <div className="flex min-h-full flex-col bg-background">
            <div className="border-b border-zinc-800 bg-zinc-900 text-zinc-50 dark:bg-zinc-950">
                <div className="mx-auto flex max-w-7xl flex-col gap-4 px-4 py-6 sm:px-6 md:flex-row md:items-center md:gap-8 md:py-4">
                    <h1 className="font-heading text-2xl font-bold md:text-3xl">
                        {t('common:navigation.myLearning')}
                    </h1>
                    {/* A 2x2 grid on a phone, a row from `sm:` up. The tabs used to sit in a
                        horizontally scrolling strip with the scrollbar hidden, which meant the ones
                        past the fold were both off-screen and unadvertised — there was nothing to
                        tell you they were there. A fourth tab made that untenable; wrapping shows all
                        of them at once and needs no affordance at all. */}
                    <nav className="grid grid-cols-2 gap-2 text-sm font-medium sm:flex sm:flex-wrap md:ml-4">
                        {tabs.map((tab) => (
                            <NavLink
                                key={tab.to}
                                to={tab.to}
                                end={tab.to === APP_ROUTES.student.myLearning}
                                className={({ isActive }) =>
                                    cn(
                                        'whitespace-nowrap rounded-lg px-4 py-2 text-center transition-colors',
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
            {/* A floor under the content, so the page keeps its height no matter which tab is open.
                Without it the page is only as tall as whatever it holds, and switching from a tab
                whose empty state carries a button to one whose empty state does not makes everything
                below jump. The reserve clears the tallest empty state; anything taller just grows. */}
            <div className="min-h-[32rem] flex-1">
                <Outlet />
            </div>
        </div>
    );
}
