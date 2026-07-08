import { Outlet } from 'react-router-dom';
import { LanguageSwitcher } from '@/components/common/ui/LanguageSwitcher';
import { ThemeSwitcher } from '@/components/common/ui/ThemeSwitcher';

export function AuthLayout() {
    return (
        <div className="relative flex min-h-screen flex-col items-center bg-background">
            <div className="relative w-full max-w-7xl">
                <div className="absolute right-4 top-4 z-10 flex items-center gap-1 sm:right-8 sm:top-6">
                    <LanguageSwitcher />
                    <ThemeSwitcher />
                </div>
            </div>
            <div className="flex w-full flex-1 items-center justify-center p-4 pt-14 sm:p-8">
                <Outlet />
            </div>
        </div>
    );
}
