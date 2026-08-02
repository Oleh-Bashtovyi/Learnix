import { useTranslation } from 'react-i18next';
import { Moon, Sun } from 'lucide-react';
import { useThemeStore } from '@/store/theme.store';
import { cn } from '@/utils/cn';

interface ThemeSwitcherProps {
    className?: string;
    variant?: 'default' | 'mobileMenu';
}

export function ThemeSwitcher({ className, variant = 'default' }: ThemeSwitcherProps) {
    const { theme, toggleTheme } = useThemeStore();
    const { t } = useTranslation('header');

    if (variant === 'mobileMenu') {
        return (
            <button
                type="button"
                onClick={toggleTheme}
                className={cn(
                    'flex w-full items-center gap-3 rounded-lg px-4 py-3 text-base font-medium text-muted-foreground transition-colors hover:bg-secondary/50 hover:text-foreground',
                    className,
                )}
            >
                {theme === 'dark' ? <Sun size={20} /> : <Moon size={20} />}
                {t('menuTheme')}
            </button>
        );
    }

    return (
        <button
            type="button"
            onClick={toggleTheme}
            aria-label="Toggle theme"
            className={cn(
                'grid size-9 place-items-center rounded-lg text-muted-foreground transition-colors hover:bg-secondary hover:text-foreground',
                className,
            )}
        >
            {theme === 'dark' ? <Sun className="size-4" /> : <Moon className="size-4" />}
        </button>
    );
}
