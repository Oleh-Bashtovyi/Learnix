const features = [
    'Streaming responses — get answers as fast as you can read them',
    "Knows the context of every lesson you've taken",
    'Explains code, debugs, and quizzes you on weak spots',
    'Conversation history saved across all your devices',
];

export function AIAssistantSection() {
    return (
        <section id="features" className="bg-foreground py-20 text-background">
            <div className="mx-auto grid max-w-7xl items-center gap-12 px-6 md:grid-cols-2">
                <div>
          <span className="inline-flex items-center gap-2 rounded-full bg-accent/20 px-3 py-1.5 text-xs font-semibold uppercase tracking-wider text-accent">
            ✨ Powered by Claude
          </span>
                    <h2 className="mt-5 font-heading text-4xl font-bold leading-tight md:text-5xl">
                        Your personal
                        <br />
                        AI tutor.
                        <br />
                        <span className="text-primary">Always on.</span>
                    </h2>
                    <p className="mt-6 max-w-lg text-lg leading-relaxed text-background/70">
                        No more hunting through Stack Overflow at 2 AM. Ask the AI tutor anything — it knows
                        your course context, your progress, and how you learn best.
                    </p>
                    <ul className="mt-8 space-y-4 text-background/80">
                        {features.map((f) => (
                            <li key={f} className="flex gap-3">
                                <span className="mt-0.5 text-success">✓</span>
                                <span>{f}</span>
                            </li>
                        ))}
                    </ul>
                </div>

                {/* Mock chat panel */}
                <div className="overflow-hidden rounded-2xl border border-border bg-card text-foreground shadow-2xl">
                    <div className="flex items-center gap-3 border-b border-border p-4">
                        <div className="grid h-10 w-10 place-items-center rounded-full bg-accent/20 text-accent">
                            ✨
                        </div>
                        <div>
                            <p className="text-sm font-medium">AI Tutor</p>
                            <p className="flex items-center gap-1 text-xs text-muted-foreground">
                                <span className="h-1.5 w-1.5 rounded-full bg-success" />
                                Active · Lesson context loaded
                            </p>
                        </div>
                    </div>

                    <div className="max-h-[420px] min-h-[340px] space-y-4 overflow-y-auto p-5">
                        <div className="flex gap-3">
                            <div className="grid h-8 w-8 shrink-0 place-items-center rounded-full bg-primary/20 text-xs font-medium">
                                YOU
                            </div>
                            <div className="max-w-[80%] rounded-xl rounded-tl-sm bg-secondary p-3 text-sm">
                                Why does <code className="rounded bg-card px-1 py-0.5 text-xs">useEffect</code> run
                                twice in development?
                            </div>
                        </div>
                        <div className="flex gap-3">
                            <div className="grid h-8 w-8 shrink-0 place-items-center rounded-full bg-accent/20 text-xs text-accent">
                                ✨
                            </div>
                            <div className="max-w-[80%] rounded-xl rounded-tl-sm bg-primary/10 p-3 text-sm">
                                Great question — this is React's <strong>Strict Mode</strong> in dev. It
                                intentionally mounts components twice to surface side-effect bugs early.
                                <div className="mt-2 text-xs text-muted-foreground">
                                    In production it runs once. Want me to show how to detect and fix common Strict
                                    Mode issues?
                                </div>
                            </div>
                        </div>
                        <div className="flex gap-3">
                            <div className="grid h-8 w-8 shrink-0 place-items-center rounded-full bg-primary/20 text-xs font-medium">
                                YOU
                            </div>
                            <div className="max-w-[80%] rounded-xl rounded-tl-sm bg-secondary p-3 text-sm">
                                Yes please
                            </div>
                        </div>
                        <div className="flex gap-3">
                            <div className="grid h-8 w-8 shrink-0 place-items-center rounded-full bg-accent/20 text-xs text-accent">
                                ✨
                            </div>
                            <div className="max-w-[80%] rounded-xl rounded-tl-sm bg-primary/10 p-3 text-sm">
                <span className="inline-flex gap-1">
                  <span className="h-1.5 w-1.5 animate-pulse rounded-full bg-accent" />
                  <span
                      className="h-1.5 w-1.5 animate-pulse rounded-full bg-accent"
                      style={{ animationDelay: '0.2s' }}
                  />
                  <span
                      className="h-1.5 w-1.5 animate-pulse rounded-full bg-accent"
                      style={{ animationDelay: '0.4s' }}
                  />
                </span>
                            </div>
                        </div>
                    </div>

                    <div className="border-t border-border bg-secondary/30 p-4">
                        <div className="flex items-center gap-2 rounded-lg border border-border bg-card px-3 py-2">
                            <input
                                type="text"
                                placeholder="Ask anything about this lesson..."
                                className="flex-1 bg-transparent text-sm outline-none"
                            />
                            <button type="button" className="text-primary">
                                ↑
                            </button>
                        </div>
                    </div>
                </div>
            </div>
        </section>
    );
}
