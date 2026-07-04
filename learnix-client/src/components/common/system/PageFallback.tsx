/**
 * Fallback shown while a lazy-loaded page chunk is being fetched.
 * Used inside <Suspense> for route-level code splitting.
 */
export function PageFallback() {
    return (
        <div className="flex min-h-[60vh] items-center justify-center text-muted-foreground">
            Loading…
        </div>
    );
}
