import { Outlet, ScrollRestoration, useLocation } from 'react-router-dom';
import { AiChatWidget } from '@/components/common/AiChatWidget/AiChatWidget';
import { EmailConfirmationBanner } from '@/components/common/auth/EmailConfirmationBanner';
import { useNotificationsHub } from '@/hooks/realtime/useNotificationsHub';
import { Footer } from './Footer';
import { Header } from './Header';

const NO_FOOTER_ROUTES = ['/messages', '/instructor/messages'];

export function PublicLayout() {
    useNotificationsHub();
    const { pathname } = useLocation();
    const hideFooter = NO_FOOTER_ROUTES.some((p) => pathname.startsWith(p));

    return (
        <div
            className={
                hideFooter ? 'flex h-screen flex-col overflow-hidden' : 'flex min-h-screen flex-col'
            }
        >
            <Header />
            <EmailConfirmationBanner />
            <main
                className={hideFooter ? 'flex min-h-0 flex-1 flex-col overflow-hidden' : 'flex-1'}
            >
                <Outlet />
            </main>
            {!hideFooter && <Footer />}
            <AiChatWidget />
            <ScrollRestoration />
        </div>
    );
}
