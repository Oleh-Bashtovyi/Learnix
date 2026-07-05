import { Outlet } from 'react-router-dom';
import { Moon, Sun } from 'lucide-react';
import { LanguageSwitcher } from '@/components/common/ui/LanguageSwitcher';
import { useThemeStore } from '@/store/theme.store';

export function AuthLayout() {
    const { theme, toggleTheme } = useThemeStore();

    return (
        <div className="relative flex min-h-screen flex-col bg-background">
            <div className="absolute right-4 top-4 z-10 flex items-center gap-2">
                <LanguageSwitcher />
                <button
                    type="button"
                    onClick={toggleTheme}
                    aria-label="Toggle theme"
                    className="grid size-9 place-items-center rounded-lg text-muted-foreground transition-colors hover:bg-secondary hover:text-foreground"
                >
                    {theme === 'dark' ? <Sun className="size-4" /> : <Moon className="size-4" />}
                </button>
            </div>
            <div className="flex flex-1 items-center justify-center p-4 pt-16 sm:p-8">
                <Outlet />
            </div>
        </div>
    );
}
