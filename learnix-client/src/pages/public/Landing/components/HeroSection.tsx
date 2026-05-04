import { Link } from 'react-router-dom';

export function HeroSection() {
    return (
        <section className="hero-blob relative overflow-hidden">
            <div className="relative mx-auto grid max-w-7xl items-center gap-12 px-6 py-20 md:grid-cols-2 md:py-28">
                <div>
          <span className="inline-flex items-center gap-2 rounded-full bg-accent/10 px-3 py-1.5 text-xs font-semibold uppercase tracking-wider text-accent">
            <span className="h-1.5 w-1.5 rounded-full bg-accent" />
            New cohorts every week
          </span>
                    <h1 className="mt-5 font-heading text-5xl font-bold leading-[1.05] md:text-6xl lg:text-7xl">
                        Learn skills that
                        <br />
                        actually <span className="text-primary">ship</span>.
                    </h1>
                    <p className="mt-6 max-w-lg text-lg leading-relaxed text-muted-foreground">
                        Project-based courses from real practitioners. A built-in AI tutor that's available
                        24/7. Certificates you can actually share.
                    </p>
                    <div className="mt-8 flex flex-wrap gap-3">
                        <Link
                            to="/courses"
                            className="rounded-lg bg-primary px-6 py-3 font-medium text-primary-foreground transition-colors hover:bg-primary/90"
                        >
                            Browse courses
                        </Link>

                        href="#instructors"
                        className="rounded-lg border border-border bg-card px-6 py-3 font-medium transition-colors hover:bg-secondary">
                        Become an instructor
                    </a>
                </div>
                <div className="mt-8 flex items-center gap-4 text-sm text-muted-foreground">
                    <div className="flex -space-x-2">
                        <div className="h-8 w-8 rounded-full border-2 border-background bg-primary/40" />
                        <div className="h-8 w-8 rounded-full border-2 border-background bg-accent/40" />
                        <div className="h-8 w-8 rounded-full border-2 border-background bg-warning/40" />
                        <div className="h-8 w-8 rounded-full border-2 border-background bg-success/40" />
                        <div className="grid h-8 w-8 place-items-center rounded-full border-2 border-background bg-card text-xs font-medium">
                            +85k
                        </div>
                    </div>
                    <span>Join 85,000+ learners worldwide</span>
                </div>
            </div>

            {/* Floating decorative cards — hidden on mobile */}
            <div className="relative hidden h-[480px] md:block">
                <div className="absolute right-0 top-4 grid aspect-video w-[85%] place-items-center rounded-2xl border border-border bg-gradient-to-br from-primary/30 via-accent/20 to-primary/10 shadow-2xl">
                    <div className="grid h-20 w-20 cursor-pointer place-items-center rounded-full bg-card shadow-xl transition-transform hover:scale-105">
                        <svg className="ml-1 h-8 w-8 text-primary" fill="currentColor" viewBox="0 0 24 24">
                            <path d="M8 5v14l11-7z" />
                        </svg>
                    </div>
                </div>

                <div className="absolute -left-2 top-44 max-w-[260px] rounded-xl border border-border bg-card p-4 shadow-xl">
                    <div className="mb-2 flex items-center gap-2 text-xs font-medium text-accent">
                        <span className="h-2 w-2 animate-pulse rounded-full bg-accent" />
                        AI Tutor
                    </div>
                    <p className="text-sm">
                        Why does <code className="rounded bg-secondary px-1 py-0.5 text-xs">useEffect</code>{' '}
                        run twice in dev mode?
                    </p>
                    <div className="mt-3 border-t border-border pt-3 text-xs text-muted-foreground">
                        Strict Mode mounts components twice to surface side-effect bugs early...
                    </div>
                </div>

                <div className="absolute bottom-8 right-8 flex items-center gap-3 rounded-xl border border-border bg-card p-4 shadow-xl">
                    <div className="grid h-12 w-12 place-items-center rounded-full bg-success/20 text-xl text-success">
                        🏆
                    </div>
                    <div>
                        <p className="text-xs text-muted-foreground">Achievement unlocked</p>
                        <p className="text-sm font-medium">Knowledge Seeker</p>
                        <p className="text-xs text-success">+50 XP</p>
                    </div>
                </div>

                <div className="absolute -left-4 bottom-32 w-[220px] rounded-xl border border-border bg-card p-4 shadow-xl">
                    <p className="text-xs text-muted-foreground">Course progress</p>
                    <p className="mt-0.5 text-sm font-medium">Advanced React Patterns</p>
                    <div className="mt-2 h-1.5 overflow-hidden rounded-full bg-secondary">
                        <div className="h-full rounded-full bg-primary" style={{ width: '68%' }} />
                    </div>
                    <p className="mt-1 text-xs text-muted-foreground">32 of 48 lessons · 68%</p>
                </div>
            </div>
        </div>
</section>
);
}