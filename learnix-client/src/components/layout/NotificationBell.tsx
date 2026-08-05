import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import { Bell } from 'lucide-react';
import { CountBadge } from '@/components/common/elements/CountBadge';
import { HEADER_ICON_SIZE } from '@/const/ui.constants';
import { useNotificationsCount } from '@/hooks/student/useNotificationsCount';
import { APP_ROUTES } from '@/routes/paths';
import { cn } from '@/utils/cn';

export function NotificationBell() {
    const { t } = useTranslation('header');

    const { data: notifData } = useNotificationsCount();

    const unread = notifData?.count ?? 0;

    return (
        <Link
            to={APP_ROUTES.student.notifications}
            className={cn(
                'relative inline-flex items-center justify-center rounded-md p-2',
                'text-muted-foreground transition-colors hover:bg-muted hover:text-foreground',
            )}
            aria-label={t('common:navigation.notifications')}
        >
            <Bell size={HEADER_ICON_SIZE.action} />
            <CountBadge count={unread} />
        </Link>
    );
}
