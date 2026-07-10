import { useEffect } from 'react';
import { useLocation } from 'react-router-dom';

/** Roughly a second at 60fps — long enough for a lazy route chunk to arrive. */
const MAX_FRAMES = 60;

/**
 * Scrolls to `location.hash` once its target exists.
 *
 * Pages are lazy-loaded, so on a cold load the browser looks for the anchor before the
 * route chunk has rendered, finds nothing, and stays at the top. Retrying across frames
 * covers that, and running on every hash change also makes in-app `Link`s to `#anchor`
 * targets work, which router navigation alone never scrolls.
 */
export function HashScroll() {
    const { hash } = useLocation();

    useEffect(() => {
        if (!hash) return;

        let frame = 0;
        let attempts = 0;

        const scrollToTarget = () => {
            // A hash can be any user-controlled string; an invalid selector must not throw.
            let target: Element | null = null;
            try {
                target = document.querySelector(hash);
            } catch {
                return;
            }

            if (target) {
                target.scrollIntoView({ behavior: 'smooth' });
                return;
            }
            if (attempts++ < MAX_FRAMES) {
                frame = requestAnimationFrame(scrollToTarget);
            }
        };

        frame = requestAnimationFrame(scrollToTarget);
        return () => cancelAnimationFrame(frame);
    }, [hash]);

    return null;
}
