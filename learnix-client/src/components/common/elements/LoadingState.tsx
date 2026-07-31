interface LoadingStateProps {
    /** Text shown under the spinner — always visible, unlike <PageFallback>'s screen-reader-only
     * label. Callers own the wrapper that positions/sizes this within the page. */
    label: string;
}

export function LoadingState({ label }: LoadingStateProps) {
    return (
        <div className="space-y-3 text-center">
            <div className="mx-auto size-10 animate-spin rounded-full border-2 border-primary border-t-transparent" />
            <p className="text-sm text-muted-foreground">{label}</p>
        </div>
    );
}
