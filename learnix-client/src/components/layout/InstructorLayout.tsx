import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { BookOpen, LayoutDashboard, MessageSquare, PlusCircle, TrendingUp } from 'lucide-react';
import { authApi } from '@/api/auth.api';
import { messagesApi } from '@/api/messages.api';
import { queryKeys } from '@/api/queryKeys';
import { AiChatWidget } from '@/components/common/AiChatWidget/AiChatWidget';
import { BrandLogo } from '@/components/common/ui/BrandLogo';
import { DashboardLayout } from '@/components/layout/DashboardLayout';
import { useNotificationsHub } from '@/hooks/realtime/useNotificationsHub';
import { APP_ROUTES } from '@/routes/paths';
import { useAuthStore } from '@/store/auth.store';

export function InstructorLayout() {
    const { t } = useTranslation('instructor');
    useNotificationsHub();
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
            to: APP_ROUTES.instructor.dashboard,
            label: t('common:navigation.dashboard'),
            icon: <LayoutDashboard size={16} />,
            end: true,
        },
        {
            to: APP_ROUTES.instructor.courses,
            label: t('navMyCourses'),
            icon: <BookOpen size={16} />,
            end: true,
        },
        {
            to: APP_ROUTES.instructor.newCourse,
            label: t('navNewCourse'),
            icon: <PlusCircle size={16} />,
        },
        {
            to: APP_ROUTES.instructor.messages,
            label: t('common:navigation.messages'),
            icon: <MessageSquare size={16} />,
            badge:
                unreadCount > 0 ? (
                    <span className="ml-auto flex h-4 min-w-4 items-center justify-center rounded-full bg-destructive px-1 text-[10px] font-bold text-destructive-foreground">
                        {unreadCount > 99 ? '99+' : unreadCount}
                    </span>
                ) : undefined,
        },
        {
            to: APP_ROUTES.instructor.earnings,
            label: t('navEarnings'),
            icon: <TrendingUp size={16} />,
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

    const InstructorLogo = <BrandLogo iconClassName="size-5" />;

    return (
        <DashboardLayout
            roleLabel={t('common:roles.instructor')}
            themeColor="primary"
            brandNode={InstructorLogo}
            navItems={navItems}
            profileLabel={t('common:navigation.myProfile')}
            backToLabel={t('common:actions.backToCatalog')}
            signOutLabel={t('common:actions.signOut')}
            onSignOut={handleSignOut}
        >
            <AiChatWidget />
        </DashboardLayout>
    );
}
