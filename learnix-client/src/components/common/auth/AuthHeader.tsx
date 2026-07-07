import { Link } from 'react-router-dom';
import { Logo } from '@/components/common/ui/Logo';
import { APP_ROUTES } from '@/routes/paths';

interface AuthHeaderProps {
    title: string;
    subtitle?: string;
}

export function AuthHeader({ title, subtitle }: AuthHeaderProps) {
    return (
        <div className="mb-8 text-center">
            <Link
                to={APP_ROUTES.public.home}
                className="mb-6 inline-flex items-center gap-2 font-heading font-bold"
            >
                <div className="grid size-9 place-items-center rounded-lg bg-primary font-heading text-lg font-bold text-primary-foreground">
                    <Logo className="size-6" />
                </div>
                <span className="text-xl">Learnix</span>
            </Link>

            <h1 className="font-heading text-2xl font-bold text-foreground">{title}</h1>

            {subtitle && <p className="mt-1.5 text-sm text-muted-foreground">{subtitle}</p>}
        </div>
    );
}
