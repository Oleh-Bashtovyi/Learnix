const apiUrl = import.meta.env.VITE_API_URL ?? 'http://localhost:5000/api';

export const env = {
    API_URL: apiUrl,
    HUB_URL: apiUrl.replace(/\/api\/?$/, ''),
} as const;
