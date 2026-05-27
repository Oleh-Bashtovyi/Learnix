import { Outlet } from 'react-router-dom';
import { Header } from './Header';
import { Footer } from './Footer';
import { AiChatWidget } from '@/components/common/AiChatWidget/AiChatWidget';
import { EmailConfirmationBanner } from '@/components/common/EmailConfirmationBanner';
import { useChatHub } from '@/hooks/useChatHub';

export function PublicLayout() {
    useChatHub();
    return (
        <div className="flex min-h-screen flex-col">
            <Header />
            <EmailConfirmationBanner />
            <main className="flex-1">
                <Outlet />
            </main>
            <Footer />
            <AiChatWidget />
        </div>
    );
}
