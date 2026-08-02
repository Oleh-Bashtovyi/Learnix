import { LoadingSpinner } from './LoadingSpinner';

interface LoadingStateProps {
    /** Text shown under the spinner — always visible, unlike <PageFallback>'s screen-reader-only
     * label. Callers own the wrapper that positions/sizes this within the page. */
    label: string;
}

export function LoadingState({ label }: LoadingStateProps) {
    return (
        <div className="space-y-3 text-center">
            <LoadingSpinner size="lg" className="p-0" />
            <p className="text-sm text-muted-foreground">{label}</p>
        </div>
    );
}
