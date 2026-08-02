import { Link } from 'react-router-dom';
import type { LucideIcon } from 'lucide-react';

interface EmptyStateProps {
    icon: LucideIcon;
    title: string;
    description: string;
    /** Optional call to action. Passed as data, not markup, so every page's button matches. */
    action?: {
        to: string;
        label: string;
    };
}

/**
 * The "nothing here yet" panel behind the My Learning tabs — enrolled courses, wishlist,
 * certificates. Only the icon, the copy and the optional action vary; sizing and spacing
 * belong to this component so the three tabs stay identical as they drift apart.
 */
export function EmptyState({ icon: Icon, title, description, action }: EmptyStateProps) {
    return (
        <div className="mt-16 text-center">
            <div className="mx-auto flex size-24 items-center justify-center rounded-full bg-accent/10">
                <Icon className="size-12 text-accent-strong" aria-hidden="true" />
            </div>
            <h2 className="mt-6 font-heading text-2xl font-bold">{title}</h2>
            <p className="mt-2 text-muted-foreground">{description}</p>
            {action && (
                <Link
                    to={action.to}
                    className="mt-6 inline-flex h-11 w-full items-center justify-center rounded-md bg-primary px-8 font-medium text-primary-foreground hover:bg-primary/90 sm:mt-8 sm:w-auto"
                >
                    {action.label}
                </Link>
            )}
        </div>
    );
}
