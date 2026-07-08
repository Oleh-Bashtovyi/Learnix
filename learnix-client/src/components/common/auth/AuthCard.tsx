import { type ReactNode } from 'react';
import { cn } from '@/utils/cn';

interface AuthCardProps {
    children: ReactNode;
    className?: string;
}

export function AuthCard({ children, className }: AuthCardProps) {
    return (
        <div className={cn('w-full max-w-[420px] py-1', className)}>
            <div className="rounded-2xl border border-border bg-card/95 p-5 shadow-2xl shadow-primary/5 backdrop-blur-sm sm:p-8">
                {children}
            </div>
        </div>
    );
}
