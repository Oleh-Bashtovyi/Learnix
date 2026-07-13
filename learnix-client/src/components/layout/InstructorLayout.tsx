import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { BookOpen, LayoutDashboard, MessageSquare, PlusCircle, TrendingUp } from 'lucide-react';
import { messagesApi } from '@/api/messages.api';
import { queryKeys } from '@/api/queryKeys';
import { BrandLogo } from '@/components/common/ui/BrandLogo';
import { CountBadge } from '@/components/common/ui/CountBadge';
import { DashboardLayout } from '@/components/layout/DashboardLayout';
import { useNotificationsHub } from '@/hooks/realtime/useNotificationsHub';
import { APP_ROUTES } from '@/routes/paths';

export function InstructorLayout() {
    const { t } = useTranslation('instructor');
    useNotificationsHub();

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
            badge: <CountBadge count={unreadCount} placement="inline" />,
        },
        {
            to: APP_ROUTES.instructor.earnings,
            label: t('navEarnings'),
            icon: <TrendingUp size={16} />,
        },
    ];

    const InstructorLogo = <BrandLogo iconClassName="size-5" />;

    return (
        // No AI widget here. The tutor's tools are a student's context — the learning profile, the
        // current lesson, a test review — none of which mean anything on a dashboard or in the course
        // editor, where the button only covered a page that does not scroll away from it. Instructors
        // still get the widget wherever they are actually learners: the public pages and the player.
        <DashboardLayout
            roleLabel={t('common:roles.instructor')}
            themeColor="primary"
            brandNode={InstructorLogo}
            navItems={navItems}
        />
    );
}
