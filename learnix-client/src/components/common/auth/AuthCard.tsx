import { type ReactNode } from 'react';
import { cn } from '@/utils/cn';

interface AuthCardProps {
    children: ReactNode;
    className?: string;
}

export function AuthCard({ children, className }: AuthCardProps) {
    return (
        <div className={cn('w-full max-w-[420px] py-1', className)}>
            <div className="rounded-2xl border border-border bg-card p-5 shadow-[0_4px_20px_rgba(59,130,246,0.05)] sm:p-8">
                {children}
            </div>
        </div>
    );
}
