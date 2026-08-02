import { useTranslation } from 'react-i18next';
import { Link, useNavigate } from 'react-router-dom';
import { ArrowLeft } from 'lucide-react';
import { useCanGoBack } from '@/hooks/shared/useCanGoBack';
import { cn } from '@/utils/cn';

interface BackLinkProps {
    /** Where to go when there is no in-app history to step back to. */
    fallbackTo: string;
    /** Label for that fallback, already translated — e.g. "Back to catalog". */
    fallbackLabel: string;
    className?: string;
}

/**
 * "Back" when the user reached this page from inside the app, and a labelled
 * link to a sensible parent page when they landed here directly.
 */
export function BackLink({ fallbackTo, fallbackLabel, className }: BackLinkProps) {
    const { t } = useTranslation('common');
    const navigate = useNavigate();
    const canGoBack = useCanGoBack();

    const classes = cn(
        'inline-flex items-center gap-1 text-sm text-muted-foreground transition-colors hover:text-foreground',
        className,
    );

    if (canGoBack) {
        return (
            <button type="button" onClick={() => navigate(-1)} className={classes}>
                <ArrowLeft className="size-4" />
                {t('actions.back')}
            </button>
        );
    }

    return (
        <Link to={fallbackTo} className={classes}>
            <ArrowLeft className="size-4" />
            {fallbackLabel}
        </Link>
    );
}
