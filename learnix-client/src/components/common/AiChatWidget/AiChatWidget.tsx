import { Bot } from 'lucide-react';
import { useLocation } from 'react-router-dom';
import { cn } from '@/utils/cn';
import { useAuthStore } from '@/store/auth.store';
import { useUiStore } from '@/store/ui.store';
import { AI_CHAT } from '@/const/localization/aiChat';
import { AiChatPanel } from './components/AiChatPanel';

const HIDDEN_ON = ['/messages', '/instructor/messages'];

export function AiChatWidget() {
    const user = useAuthStore((s) => s.user);
    const { isChatOpen, toggleChat, closeChat } = useUiStore();
    const { pathname } = useLocation();

    if (!user || HIDDEN_ON.some((p) => pathname.startsWith(p))) return null;

    return (
        <div className="fixed bottom-6 right-6 z-50">
            <div className="relative">
                <AiChatPanel isOpen={isChatOpen} onClose={closeChat} />

                <button
                    onClick={toggleChat}
                    aria-label={AI_CHAT.ARIA_TOGGLE}
                    aria-expanded={isChatOpen}
                    className={cn(
                        'flex h-14 w-14 items-center justify-center rounded-full shadow-lg transition-all duration-200',
                        'bg-primary text-primary-foreground hover:bg-primary/90',
                        'focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary focus-visible:ring-offset-2',
                        isChatOpen && 'rotate-12 scale-95',
                    )}
                >
                    <Bot size={24} />
                </button>
            </div>
        </div>
    );
}
