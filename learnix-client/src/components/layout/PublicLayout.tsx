import { Outlet } from 'react-router-dom';
import { Header } from './Header';
import { Footer } from './Footer';
import { AiChatWidget } from '@/components/common/AiChatWidget/AiChatWidget';

export function PublicLayout() {
    return (
        <div className="flex min-h-screen flex-col">
            <Header />
            <main className="flex-1">
                <Outlet />
            </main>
            <Footer />
            <AiChatWidget />
        </div>
    );
}
