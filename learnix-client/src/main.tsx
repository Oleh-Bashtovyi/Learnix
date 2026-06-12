import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { HelmetProvider } from 'react-helmet-async';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { GoogleOAuthProvider } from '@react-oauth/google';
import { toast, Toaster } from 'sonner';
import App from './App';
import { AuthInitializer } from '@/components/common/AuthInitializer';
import { ErrorBoundary } from '@/components/common/ErrorBoundary';
import { isValidationError, getErrorMessage } from '@/utils/errors';
import '@/i18n/config';
import '@fontsource/dm-sans/400.css';
import '@fontsource/dm-sans/500.css';
import '@fontsource/dm-sans/600.css';
import '@fontsource/dm-sans/700.css';
import '@fontsource/plus-jakarta-sans/600.css';
import '@fontsource/plus-jakarta-sans/700.css';
import '@fontsource/plus-jakarta-sans/800.css';
import '@/styles/index.css';

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
                if (mutation.meta?.suppressGlobalError) return;
                if (!isValidationError(error)) {
                    toast.error(getErrorMessage(error));
                }
            },
        },
    },
});

createRoot(document.getElementById('root')!).render(
    <StrictMode>
        <HelmetProvider>
            <ErrorBoundary>
                <GoogleOAuthProvider clientId={import.meta.env.VITE_GOOGLE_CLIENT_ID}>
                    <QueryClientProvider client={queryClient}>
                        <AuthInitializer>
                            <App />
                            <Toaster position="top-right" richColors />
                        </AuthInitializer>
                    </QueryClientProvider>
                </GoogleOAuthProvider>
            </ErrorBoundary>
        </HelmetProvider>
    </StrictMode>,
);
