import { Link } from 'react-router-dom';

interface AuthFooterProps {
    text?: string;
    linkText: string;
    linkTo: string;
    linkState?: unknown;
}

export function AuthFooter({ text, linkText, linkTo, linkState }: AuthFooterProps) {
    const linkClass = text
        ? 'font-medium text-primary hover:underline'
        : 'text-sm font-medium text-primary hover:underline';

    if (text) {
        return (
            <p className="mt-6 text-center text-sm text-muted-foreground">
                {text}{' '}
                <Link to={linkTo} state={linkState} className={linkClass}>
                    {linkText}
                </Link>
            </p>
        );
    }

    return (
        <div className="mt-6 text-center">
            <Link to={linkTo} state={linkState} className={linkClass}>
                {linkText}
            </Link>
        </div>
    );
}
