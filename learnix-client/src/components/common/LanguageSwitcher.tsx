import { useLocaleStore } from '@/store/locale.store';
import { cn } from '@/utils/cn';

const LABELS: Record<string, string> = { en: 'EN', uk: 'UK' };

interface LanguageSwitcherProps {
    className?: string;
}

export function LanguageSwitcher({ className }: LanguageSwitcherProps) {
    const { language, setLanguage } = useLocaleStore();

    return (
        <div
            className={cn(
                'flex items-center gap-0.5 rounded-md border border-border p-0.5',
                className,
            )}
        >
            {(['en', 'uk'] as const).map((lang) => (
                <button
                    key={lang}
                    onClick={() => setLanguage(lang)}
                    className={cn(
                        'rounded px-2 py-0.5 text-xs font-medium transition-colors',
                        language === lang
                            ? 'bg-primary text-primary-foreground'
                            : 'text-muted-foreground hover:text-foreground',
                    )}
                    aria-pressed={language === lang}
                >
                    {LABELS[lang]}
                </button>
            ))}
        </div>
    );
}
