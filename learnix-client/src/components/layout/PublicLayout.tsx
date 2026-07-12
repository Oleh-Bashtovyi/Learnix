import { Outlet, ScrollRestoration, matchPath, useLocation } from 'react-router-dom';
import { AiChatWidget } from '@/components/common/AiChatWidget/AiChatWidget';
import { EmailConfirmationBanner } from '@/components/common/auth/EmailConfirmationBanner';
import { HashScroll } from '@/components/common/system/HashScroll';
import { useNotificationsHub } from '@/hooks/realtime/useNotificationsHub';
import { APP_ROUTES } from '@/routes/paths';
import { cn } from '@/utils/cn';
import { Footer } from './Footer';
import { Header } from './Header';

const NO_FOOTER_ROUTES = [APP_ROUTES.student.messages, APP_ROUTES.instructor.messages];

export function PublicLayout() {
    useNotificationsHub();
    const { pathname } = useLocation();
    const hideFooter = NO_FOOTER_ROUTES.some((p) => pathname.startsWith(p));
    const hideChatWidget = !!matchPath(APP_ROUTES.public.verifyCertificatePattern, pathname);

    // h-dvh, not h-screen: 100vh ignores the mobile browser's URL bar, so a shell meant to fill the
    // screen exactly ends up taller than it and the page gains a scrollbar of its own — which is what
    // pushed the message composer below the fold on a phone.
    return (
        <div
            className={
                hideFooter ? 'flex h-dvh flex-col overflow-hidden' : 'flex min-h-dvh flex-col'
            }
        >
            <EmailConfirmationBanner />
            <Header />
            <main
                className={
                    hideFooter
                        ? 'flex min-h-0 flex-1 flex-col overflow-hidden'
                        : cn('flex-1', !hideChatWidget && 'pb-24 md:pb-8')
                }
            >
                <Outlet />
            </main>
            {!hideFooter && <Footer />}
            {!hideChatWidget && <AiChatWidget />}
            <ScrollRestoration />
            <HashScroll />
        </div>
    );
}
