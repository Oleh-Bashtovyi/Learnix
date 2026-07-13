import type { ReactNode } from 'react';
import { cn } from '@/utils/cn';

interface HeroPanelProps {
    children: ReactNode;
    className?: string;
}

/**
 * The decorated panel the platform's page headers are built on: a faint grid, masked so it fades out
 * downwards, over two soft brand/accent glows. Lifted from the landing hero so a header on any page
 * reads as part of the same site rather than as a card that wandered in from a different one.
 */
export function HeroPanel({ children, className }: HeroPanelProps) {
    return (
        <section
            className={cn(
                'relative overflow-hidden rounded-2xl border border-border bg-card',
                className,
            )}
        >
            <div
                aria-hidden
                className="absolute inset-0 bg-[linear-gradient(to_right,#80808012_1px,transparent_1px),linear-gradient(to_bottom,#80808012_1px,transparent_1px)] bg-[size:24px_24px] [mask-image:radial-gradient(ellipse_70%_60%_at_50%_0%,#000_60%,transparent_100%)]"
            />
            <div
                aria-hidden
                className="absolute -left-24 -top-32 size-72 rounded-full bg-brand/10 blur-3xl"
            />
            <div
                aria-hidden
                className="absolute -right-24 -top-24 size-72 rounded-full bg-accent/10 blur-3xl"
            />

            <div className="relative p-6 sm:p-8">{children}</div>
        </section>
    );
}
