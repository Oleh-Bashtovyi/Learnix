import { Component, type ReactNode } from 'react';
import { AlertTriangle, RefreshCw } from 'lucide-react';
import i18next from 'i18next';

interface Props {
    children: ReactNode;
    fallback?: ReactNode;
}

interface State {
    hasError: boolean;
}

export class ErrorBoundary extends Component<Props, State> {
    state: State = { hasError: false };

    static getDerivedStateFromError(): State {
        return { hasError: true };
    }

    componentDidCatch(error: Error, info: React.ErrorInfo) {
        if (import.meta.env.DEV) {
            console.error('ErrorBoundary caught:', error, info);
        }
    }

    render() {
        if (this.state.hasError) {
            return (
                this.props.fallback ?? (
                    <div className="flex min-h-[60vh] flex-col items-center justify-center gap-4 px-4 text-center">
                        <div className="flex h-16 w-16 items-center justify-center rounded-full bg-destructive/10">
                            <AlertTriangle className="h-8 w-8 text-destructive" />
                        </div>
                        <div>
                            <h2 className="font-heading text-xl font-semibold text-foreground">
                                {i18next.t('common:errorBoundaryTitle')}
                            </h2>
                            <p className="mt-1 text-sm text-muted-foreground">
                                {i18next.t('common:errorBoundarySubtitle')}
                            </p>
                        </div>
                        <button
                            type="button"
                            onClick={() => window.location.reload()}
                            className="inline-flex items-center gap-2 rounded-lg bg-primary px-4 py-2 text-sm font-medium text-primary-foreground hover:bg-primary/90"
                        >
                            <RefreshCw className="h-4 w-4" />
                            {i18next.t('common:errorBoundaryRefresh')}
                        </button>
                    </div>
                )
            );
        }

        return this.props.children;
    }
}
