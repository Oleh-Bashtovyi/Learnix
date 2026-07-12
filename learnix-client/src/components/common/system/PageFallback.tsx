import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { LoadingSpinner } from '@/components/common/ui/LoadingSpinner';

/** A chunk that arrives faster than this is not worth announcing — showing anything would only flash. */
const SPINNER_DELAY_MS = 150;

/**
 * Fallback shown while a lazy-loaded page chunk is being fetched.
 * Used inside <Suspense> for route-level code splitting.
 *
 * The label is for screen readers only: sighted users read the spinner, and a route chunk on a warm
 * cache is gone in a blink — text that appears and vanishes reads as a glitch rather than as progress.
 */
export function PageFallback() {
    const { t } = useTranslation('common');
    const [visible, setVisible] = useState(false);

    useEffect(() => {
        const timer = setTimeout(() => setVisible(true), SPINNER_DELAY_MS);
        return () => clearTimeout(timer);
    }, []);

    return (
        <output aria-live="polite" className="flex min-h-[60vh] items-center justify-center">
            {visible && <LoadingSpinner />}
            <span className="sr-only">{t('status.loading')}</span>
        </output>
    );
}
