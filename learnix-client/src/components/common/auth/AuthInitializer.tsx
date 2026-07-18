import { useEffect } from 'react';
import { useAuthStore } from '@/store/auth.store';
import { refreshSession } from '@/utils/refreshSession';

type AuthInitializerProps = {
    children: React.ReactNode;
};

/**
 * Related ADRs:
 * - ADR-FRONT-AUTH-001: Access Token Storage & Silent Refresh
 */
let refreshPromise: Promise<unknown> | null = null;

export function AuthInitializer({ children }: AuthInitializerProps) {
    const finishInitialization = useAuthStore((s) => s.finishInitialization);

    useEffect(() => {
        if (!refreshPromise) {
            refreshPromise = refreshSession()
                .catch(() => {
                    // No valid refresh token — user is not logged in.
                })
                .finally(() => {
                    refreshPromise = null;
                });
        } else {
            return;
        }

        refreshPromise.finally(() => {
            finishInitialization();
        });
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, []);

    return <>{children}</>;
}
