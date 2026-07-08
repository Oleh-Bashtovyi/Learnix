import { BrandLogo } from '@/components/common/ui/BrandLogo';

interface AuthHeaderProps {
    title: string;
    subtitle?: string;
}

export function AuthHeader({ title, subtitle }: AuthHeaderProps) {
    return (
        <div className="mb-8 text-center">
            <BrandLogo className="mb-4" boxClassName="size-9" textClassName="text-xl" />

            <h1 className="font-heading text-2xl font-bold text-foreground">{title}</h1>

            {subtitle && <p className="mt-2 text-sm text-foreground/80">{subtitle}</p>}
        </div>
    );
}
