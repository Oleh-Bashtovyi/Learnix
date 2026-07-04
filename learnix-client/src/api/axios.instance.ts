import axios, { type AxiosError, type InternalAxiosRequestConfig } from 'axios';
import { APP_ROUTES } from '@/routes/paths';
import { useAuthStore } from '@/store/auth.store';
import { env } from '@/utils/env';

/**
 * Related ADRs:
 * - ADR-FRONT-API-001: API Layer — Axios Instance with Queued Token Refresh
 */
export const api = axios.create({
    baseURL: env.API_URL,
    withCredentials: true,
});

api.interceptors.request.use((config) => {
    const token = useAuthStore.getState().accessToken;
    if (token) config.headers.Authorization = `Bearer ${token}`;
    return config;
});

let isRefreshing = false;
let failedQueue: Array<{
    resolve: (token: string) => void;
    reject: (err: unknown) => void;
}> = [];

const processQueue = (error: unknown, token: string | null = null) => {
    failedQueue.forEach((p) => (error ? p.reject(error) : p.resolve(token!)));
    failedQueue = [];
};

api.interceptors.response.use(
    (response) => response,
    async (error: AxiosError) => {
        const original = error.config as InternalAxiosRequestConfig & { _retry?: boolean };

        const hasBearer = original.headers?.Authorization?.toString().startsWith('Bearer ');
        if (error.response?.status !== 401 || original._retry || !hasBearer) {
            return Promise.reject(error);
        }

        if (isRefreshing) {
            return new Promise((resolve, reject) => {
                failedQueue.push({
                    resolve: (token) => {
                        original.headers.Authorization = `Bearer ${token}`;
                        resolve(api(original));
                    },
                    reject,
                });
            });
        }

        original._retry = true;
        isRefreshing = true;

        try {
            const { data } = await axios.post(
                `${env.API_URL}/auth/refresh`,
                {},
                { withCredentials: true },
            );
            useAuthStore.getState().setAccessToken(data.accessToken);
            processQueue(null, data.accessToken);
            original.headers.Authorization = `Bearer ${data.accessToken}`;
            return api(original);
        } catch (refreshError) {
            processQueue(refreshError, null);
            useAuthStore.getState().logout();
            window.location.href = APP_ROUTES.public.login;
            return Promise.reject(refreshError);
        } finally {
            isRefreshing = false;
        }
    },
);
