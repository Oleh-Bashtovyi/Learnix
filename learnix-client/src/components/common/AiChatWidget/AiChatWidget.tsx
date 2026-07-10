import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useLocation } from 'react-router-dom';
import { Bot } from 'lucide-react';
import { APP_ROUTES } from '@/routes/paths';
import { useAuthStore } from '@/store/auth.store';
import { useUiStore } from '@/store/ui.store';
import { cn } from '@/utils/cn';
import { AiChatPanel } from './components/AiChatPanel';

const HIDDEN_ON = [
    APP_ROUTES.student.messages,
    APP_ROUTES.instructor.messages,
    APP_ROUTES.admin.messages,
];

export function AiChatWidget() {
    const { t } = useTranslation('aiChat');
    const user = useAuthStore((s) => s.user);
    const { isChatOpen, toggleChat, closeChat } = useUiStore();
    const { pathname } = useLocation();
    const [isExpanded, setIsExpanded] = useState(false);
    const [prevPathname, setPrevPathname] = useState(pathname);

    if (pathname !== prevPathname) {
        setPrevPathname(pathname);
        setIsExpanded(false);
    }

    useEffect(() => {
        closeChat();
    }, [pathname, closeChat]);

    if (!user || HIDDEN_ON.some((p) => pathname.startsWith(p))) return null;

    function handleClose() {
        setIsExpanded(false);
        closeChat();
    }

    return (
        <div className="fixed bottom-4 right-4 z-50 sm:bottom-6 sm:right-6">
            <AiChatPanel
                isOpen={isChatOpen}
                onClose={handleClose}
                isExpanded={isExpanded}
                onToggleExpand={() => setIsExpanded((v) => !v)}
            />

            {!isExpanded && (
                <button
                    onClick={toggleChat}
                    aria-label={t('ariaToggle')}
                    aria-expanded={isChatOpen}
                    className={cn(
                        'flex size-[3.75rem] items-center justify-center rounded-full shadow-lg transition-all duration-200',
                        // Brand, not primary: the widget floats over the panel sections too,
                        // and --primary collapses into their background in dark mode.
                        'bg-brand text-brand-foreground hover:bg-brand/90',
                        'focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand focus-visible:ring-offset-2',
                        isChatOpen && 'rotate-12 scale-95',
                    )}
                >
                    <Bot className="size-8" />
                </button>
            )}
        </div>
    );
}
