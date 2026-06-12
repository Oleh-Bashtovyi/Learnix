import { Outlet } from 'react-router-dom';
import { Sun, Moon } from 'lucide-react';
import { useThemeStore } from '@/store/theme.store';
import { LanguageSwitcher } from '@/components/common/LanguageSwitcher';

export function AuthLayout() {
    const { theme, toggleTheme } = useThemeStore();

    return (
        <div className="relative flex min-h-screen items-center justify-center bg-background px-4">
            <div className="absolute right-4 top-4 flex items-center gap-2">
                <LanguageSwitcher />
                <button
                    type="button"
                    onClick={toggleTheme}
                    aria-label="Toggle theme"
                    className="grid h-9 w-9 place-items-center rounded-lg text-muted-foreground transition-colors hover:bg-secondary hover:text-foreground"
                >
                    {theme === 'dark' ? <Sun className="h-4 w-4" /> : <Moon className="h-4 w-4" />}
                </button>
            </div>
            <Outlet />
        </div>
    );
}
