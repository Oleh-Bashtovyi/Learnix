import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import {
    BookOpen,
    CreditCard,
    FileCheck,
    LayoutDashboard,
    MessageSquare,
    ShieldCheck,
    Tag,
    Users,
} from 'lucide-react';
import { messagesApi } from '@/api/messages.api';
import { queryKeys } from '@/api/queryKeys';
import { CountBadge } from '@/components/common/elements/CountBadge';
import { DashboardLayout } from '@/components/layout/DashboardLayout';
import { SIDEBAR_ICON_SIZE, SIDEBAR_LOGO_ICON_SIZE } from '@/const/ui.constants';
import { APP_ROUTES } from '@/routes/paths';

export function AdminLayout() {
    const { t } = useTranslation('admin');

    const { data: unreadData } = useQuery({
        queryKey: queryKeys.messages.unreadCount(),
        queryFn: messagesApi.getUnreadCount,
        staleTime: Infinity,
    });
    const unreadCount = unreadData?.totalUnread ?? 0;

    const navItems = [
        {
            to: APP_ROUTES.admin.dashboard,
            label: t('common:navigation.dashboard'),
            icon: <LayoutDashboard size={SIDEBAR_ICON_SIZE.navItem} />,
            end: true,
        },
        {
            to: APP_ROUTES.admin.users,
            label: t('navUsers'),
            icon: <Users size={SIDEBAR_ICON_SIZE.navItem} />,
        },
        {
            to: APP_ROUTES.admin.courses,
            label: t('common:navigation.courses'),
            icon: <BookOpen size={SIDEBAR_ICON_SIZE.navItem} />,
        },
        {
            to: APP_ROUTES.admin.applications,
            label: t('navApplications'),
            icon: <FileCheck size={SIDEBAR_ICON_SIZE.navItem} />,
        },
        {
            to: APP_ROUTES.admin.payments,
            label: t('navPayments'),
            icon: <CreditCard size={SIDEBAR_ICON_SIZE.navItem} />,
        },
        {
            to: APP_ROUTES.admin.categories,
            label: t('navCategories'),
            icon: <Tag size={SIDEBAR_ICON_SIZE.navItem} />,
        },
        {
            to: APP_ROUTES.admin.messages,
            label: t('common:navigation.messages'),
            icon: <MessageSquare size={SIDEBAR_ICON_SIZE.navItem} />,
            badge: <CountBadge count={unreadCount} placement="inline" />,
        },
    ];

    const AdminLogo = (
        <Link
            to={APP_ROUTES.public.home}
            className="flex items-center gap-2.5 font-heading font-bold text-foreground transition-opacity hover:opacity-90"
        >
            <div className="grid size-8 place-items-center rounded-lg bg-destructive text-destructive-foreground">
                <ShieldCheck size={SIDEBAR_LOGO_ICON_SIZE} strokeWidth={2.5} />
            </div>
            <span className="tracking-tight">Learnix</span>
        </Link>
    );

    return (
        <DashboardLayout
            roleLabel={t('common:roles.admin')}
            themeColor="destructive"
            brandNode={AdminLogo}
            navItems={navItems}
        />
    );
}
