import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import {
    BookOpen,
    CreditCard,
    FileCheck,
    LayoutDashboard,
    MessageSquare,
    Tag,
    Users,
} from 'lucide-react';
import { authApi } from '@/api/auth.api';
import { messagesApi } from '@/api/messages.api';
import { queryKeys } from '@/api/queryKeys';
import { DashboardLayout } from '@/components/layout/DashboardLayout';
import { APP_ROUTES } from '@/routes/paths';
import { useAuthStore } from '@/store/auth.store';

export function AdminLayout() {
    const { t } = useTranslation('admin');
    const navigate = useNavigate();
    const { logout } = useAuthStore();
    const queryClient = useQueryClient();

    const { data: unreadData } = useQuery({
        queryKey: queryKeys.messages.unreadCount(),
        queryFn: messagesApi.getUnreadCount,
        staleTime: Infinity,
    });
    const unreadCount = unreadData?.totalUnread ?? 0;

    const navItems = [
        {
            to: APP_ROUTES.admin.dashboard,
            label: t('navDashboard'),
            icon: <LayoutDashboard size={16} />,
            end: true,
        },
        { to: APP_ROUTES.admin.users, label: t('navUsers'), icon: <Users size={16} /> },
        { to: APP_ROUTES.admin.courses, label: t('navCourses'), icon: <BookOpen size={16} /> },
        {
            to: APP_ROUTES.admin.applications,
            label: t('navApplications'),
            icon: <FileCheck size={16} />,
        },
        { to: APP_ROUTES.admin.payments, label: t('navPayments'), icon: <CreditCard size={16} /> },
        { to: APP_ROUTES.admin.categories, label: t('navCategories'), icon: <Tag size={16} /> },
        {
            to: APP_ROUTES.admin.messages,
            label: t('navMessages'),
            icon: <MessageSquare size={16} />,
            badge:
                unreadCount > 0 ? (
                    <span className="ml-auto flex h-4 min-w-4 items-center justify-center rounded-full bg-destructive px-1 text-[10px] font-bold text-destructive-foreground">
                        {unreadCount > 99 ? '99+' : unreadCount}
                    </span>
                ) : undefined,
        },
    ];

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

    const AdminLogo = (
        <div className="grid size-8 place-items-center rounded-lg bg-destructive text-sm font-bold text-destructive-foreground">
            A
        </div>
    );

    return (
        <DashboardLayout
            roleLabel="Admin"
            themeColor="destructive"
            logoNode={AdminLogo}
            logoText="Learnix Admin"
            navItems={navItems}
            profileLabel={t('navProfile')}
            backToLabel={t('navBackToSite')}
            signOutLabel={t('navSignOut')}
            onSignOut={handleSignOut}
        />
    );
}
