import { useTranslation } from 'react-i18next';
import { Globe } from 'lucide-react';
import { Button } from '@/components/ui/button';
import {
    DropdownMenu,
    DropdownMenuContent,
    DropdownMenuItem,
    DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import { useLocaleStore } from '@/store/locale.store';
import { cn } from '@/utils/cn';

const LABELS: Record<string, string> = { en: 'English', uk: 'Українська' };
const SHORT_LABELS: Record<string, string> = { en: 'EN', uk: 'UK' };

interface LanguageSwitcherProps {
    className?: string;
    variant?: 'default' | 'mobileMenu';
}

export function LanguageSwitcher({ className, variant = 'default' }: LanguageSwitcherProps) {
    const { language, setLanguage } = useLocaleStore();
    const { t } = useTranslation('header');

    return (
        <>
            {variant === 'mobileMenu' ? (
                <DropdownMenu>
                    <DropdownMenuTrigger asChild>
                        <button className="flex w-full items-center justify-between rounded-lg px-4 py-3 text-base font-medium text-muted-foreground outline-none transition-colors hover:bg-secondary/50 hover:text-foreground">
                            <span className="flex items-center gap-3">
                                <Globe size={20} />
                                {t('menuLanguage', { defaultValue: 'Language' })}
                            </span>
                            <span className="rounded-md bg-secondary/50 px-2 py-1 text-xs font-bold uppercase text-foreground">
                                {SHORT_LABELS[language] || 'EN'}
                            </span>
                        </button>
                    </DropdownMenuTrigger>
                    <DropdownMenuContent align="end" className="w-[200px] p-2">
                        {(['en', 'uk'] as const).map((lang) => (
                            <DropdownMenuItem
                                key={lang}
                                onClick={() => setLanguage(lang)}
                                className={cn(
                                    'mb-1 cursor-pointer rounded-md px-4 py-3.5 text-base font-medium last:mb-0',
                                    language === lang && 'bg-secondary',
                                )}
                            >
                                {LABELS[lang]}
                            </DropdownMenuItem>
                        ))}
                    </DropdownMenuContent>
                </DropdownMenu>
            ) : (
                <DropdownMenu>
                    <DropdownMenuTrigger asChild>
                        <Button
                            variant="ghost"
                            className={cn(
                                'h-9 gap-2 px-2.5 text-xs font-medium text-muted-foreground hover:bg-secondary hover:text-foreground',
                                className,
                            )}
                        >
                            <Globe size={16} />
                            <span>{SHORT_LABELS[language] || 'EN'}</span>
                        </Button>
                    </DropdownMenuTrigger>
                    <DropdownMenuContent align="end" className="min-w-[150px]">
                        {(['en', 'uk'] as const).map((lang) => (
                            <DropdownMenuItem
                                key={lang}
                                onClick={() => setLanguage(lang)}
                                className={cn(
                                    'cursor-pointer py-2 text-sm',
                                    language === lang && 'bg-secondary font-medium',
                                )}
                            >
                                {LABELS[lang]}
                            </DropdownMenuItem>
                        ))}
                    </DropdownMenuContent>
                </DropdownMenu>
            )}
        </>
    );
}
