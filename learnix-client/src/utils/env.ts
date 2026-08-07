/**
 * Related ADRs:
 * - ADR-FRONT-API-006: Environment Variables Management
 */
const apiUrl = import.meta.env.VITE_API_URL;
if (!apiUrl) throw new Error('Missing env variable: VITE_API_URL');

// Optional: the public origin the app is served from. Only needed when the deployed origin differs
// from the one crawlers hit (e.g. behind a custom domain). Falls back to the runtime origin.
const siteUrl = (import.meta.env.VITE_SITE_URL || window.location.origin).replace(/\/+$/, '');

// Optional: toggles the "portfolio project" notice strip on the landing page and FAQ.
// Shown by default; set VITE_SHOW_PROJECT_BANNER=false to hide it (e.g. for a clean demo).
const showProjectBanner = import.meta.env.VITE_SHOW_PROJECT_BANNER !== 'false';

export const env = {
    API_URL: apiUrl,
    HUB_URL: apiUrl.replace(/\/api(\/v\d+)?\/?$/, ''),
    SITE_URL: siteUrl,
    SHOW_PROJECT_BANNER: showProjectBanner,
} as const;

/** Same host as `API_URL`, on a different API version — e.g. `apiUrlForVersion(2)` → `.../api/v2`. */
export function apiUrlForVersion(version: number): string {
    return env.API_URL.replace(/\/v\d+$/, `/v${version}`);
}
