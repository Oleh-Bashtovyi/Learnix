import { Link } from 'react-router-dom';
import { APP_ROUTES } from '@/routes/paths';

export default function NotFoundPage() {
    return (
        <div className="mx-auto flex min-h-[60vh] max-w-md flex-col items-center justify-center px-6 text-center">
            <p className="font-heading text-7xl font-bold text-primary">404</p>
            <h1 className="mt-4 font-heading text-2xl font-semibold">Page not found</h1>
            <p className="mt-3 text-muted-foreground">
                The page you're looking for doesn't exist or was moved.
            </p>
            <Link
                to={APP_ROUTES.public.home}
                className="mt-8 rounded-lg bg-primary px-5 py-2.5 text-sm font-medium text-primary-foreground hover:bg-primary/90"
            >
                Back home
            </Link>
        </div>
    );
}
