import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { HelmetProvider } from 'react-helmet-async';
import '@fontsource/dm-sans/400.css';
import '@fontsource/dm-sans/500.css';
import '@fontsource/dm-sans/600.css';
import '@fontsource/dm-sans/700.css';
import '@fontsource/plus-jakarta-sans/600.css';
import '@fontsource/plus-jakarta-sans/700.css';
import '@fontsource/plus-jakarta-sans/800.css';
import { GoogleOAuthProvider } from '@react-oauth/google';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { toast } from 'sonner';
import { AuthInitializer } from '@/components/common/auth/AuthInitializer';
import { AppToaster } from '@/components/common/system/AppToaster';
import { ErrorBoundary } from '@/components/common/system/ErrorBoundary';
import '@/i18n/config';
import '@/styles/index.css';
import { getErrorMessage, isValidationError } from '@/utils/errors';
import App from './App';

/**
 * Related ADRs:
 * - ADR-FRONT-API-003: React Query Structure & Defaults
 */
const queryClient = new QueryClient({
    defaultOptions: {
        queries: {
            staleTime: 1000 * 60,
            gcTime: 1000 * 60 * 5,
            retry: 1,
            refetchOnWindowFocus: false,
        },
        mutations: {
            onError: (error, _variables, _context, mutation) => {
                /**
                 * Related ADRs:
                 * - ADR-FRONT-FORMS-004: Form Errors vs Global Errors
                 * - ADR-BACK-AUTH-018: 403s carry a machine `code`; the client branches on it
                 */
                if (mutation.meta?.suppressGlobalError) return;
                if (isValidationError(error)) return;

                toast.error(getErrorMessage(error));
            },
        },
    },
});

const googleClientId =
    import.meta.env.VITE_GOOGLE_CLIENT_ID || 'dummy_to_prevent_crash.apps.googleusercontent.com';

/**
 * The fallback social tags in index.html exist purely for scrapers that never run JS. Once React
 * boots, <Seo /> owns the head — and React 19 hoists its tags without deduplicating against the
 * static ones, so they must be removed or every page would carry two conflicting og:title/og:url.
 */
document.head.querySelectorAll('meta[data-seo-fallback]').forEach((tag) => tag.remove());

createRoot(document.getElementById('root')!).render(
    <StrictMode>
        <HelmetProvider>
            <ErrorBoundary>
                <GoogleOAuthProvider clientId={googleClientId}>
                    <QueryClientProvider client={queryClient}>
                        <AuthInitializer>
                            <App />
                            <AppToaster />
                        </AuthInitializer>
                    </QueryClientProvider>
                </GoogleOAuthProvider>
            </ErrorBoundary>
        </HelmetProvider>
    </StrictMode>,
);
