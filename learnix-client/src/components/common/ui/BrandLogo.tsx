import { Link } from 'react-router-dom';
import { Logo } from '@/components/common/ui/Logo';
import { APP_ROUTES } from '@/routes/paths';
import { cn } from '@/utils/cn';

export interface BrandLogoProps {
    className?: string;
    boxClassName?: string;
    iconClassName?: string;
    textClassName?: string;
    showText?: boolean;
    onClick?: () => void;
}

export function BrandLogo({
    className,
    boxClassName = 'size-8',
    iconClassName = 'size-6',
    textClassName = 'text-lg',
    showText = true,
    onClick,
}: BrandLogoProps) {
    return (
        <Link
            to={APP_ROUTES.public.home}
            onClick={onClick}
            className={cn(
                'inline-flex items-center gap-2.5 font-heading font-bold transition-opacity hover:opacity-90',
                className,
            )}
        >
            <div
                className={cn(
                    'grid place-items-center rounded-lg bg-brand text-brand-foreground shadow-sm',
                    boxClassName,
                )}
            >
                <Logo className={iconClassName} />
            </div>
            {showText && (
                <span className={cn('tracking-tight text-foreground', textClassName)}>Learnix</span>
            )}
        </Link>
    );
}
