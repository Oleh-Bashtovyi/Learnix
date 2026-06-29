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
}

export function LanguageSwitcher({ className }: LanguageSwitcherProps) {
    const { language, setLanguage } = useLocaleStore();

    return (
        <DropdownMenu>
            <DropdownMenuTrigger asChild>
                <Button
                    variant="outline"
                    size="sm"
                    className={cn('h-8 gap-2 px-2 text-xs', className)}
                >
                    <Globe size={14} />
                    <span>{SHORT_LABELS[language] || 'EN'}</span>
                </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end">
                {(['en', 'uk'] as const).map((lang) => (
                    <DropdownMenuItem
                        key={lang}
                        onClick={() => setLanguage(lang)}
                        className={cn(language === lang && 'bg-secondary font-medium')}
                    >
                        {LABELS[lang]}
                    </DropdownMenuItem>
                ))}
            </DropdownMenuContent>
        </DropdownMenu>
    );
}
