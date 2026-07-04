/**
 * Related ADRs:
 * - ADR-FRONT-API-006: Environment Variables Management
 */
const apiUrl = import.meta.env.VITE_API_URL;
if (!apiUrl) throw new Error('Missing env variable: VITE_API_URL');

export const env = {
    API_URL: apiUrl,
    HUB_URL: apiUrl.replace(/\/api\/?$/, ''),
} as const;
