import type { Location } from 'react-router-dom';

/**
 * Standardized type for React Router state object when navigating with a `from` path.
 * Used for post-login/post-register redirection back to the intended page.
 */
export interface LocationStateWithFrom {
    from?: Location;
}
