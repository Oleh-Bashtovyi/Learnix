const enrollmentBars = [30, 50, 40, 65, 55, 75, 60, 85, 70, 90, 80, 95, 88, 100];

export function InstructorsCTASection() {
    return (
        <section
            id="instructors"
            className="bg-gradient-to-br from-accent/10 via-background to-primary/10 py-20"
        >
            <div className="mx-auto max-w-7xl px-6">
                <div className="grid items-center gap-12 rounded-3xl border border-border bg-card p-10 shadow-xl md:grid-cols-2 md:p-16">
                    <div>
                        <span className="text-sm font-semibold text-accent">For instructors</span>
                        <h2 className="mt-2 font-heading text-3xl font-bold md:text-4xl">
                            Teach what you know.
                            <br />
                            Earn while you sleep.
                        </h2>
                        <p className="mt-5 leading-relaxed text-muted-foreground">
                            Share your expertise with thousands of learners. Our editor handles video hosting,
                            payments, and certificates — so you can focus on creating great content.
                        </p>

                        <div className="mt-8 grid grid-cols-2 gap-4">
                            <div>
                                <p className="font-heading text-2xl font-bold text-foreground">$2.4k</p>
                                <p className="mt-1 text-xs text-muted-foreground">
                                    Average monthly earnings (top 100 instructors)
                                </p>
                            </div>
                            <div>
                                <p className="font-heading text-2xl font-bold text-foreground">85%</p>
                                <p className="mt-1 text-xs text-muted-foreground">
                                    Revenue share — among the highest in the industry
                                </p>
                            </div>
                        </div>

                        <div className="mt-8 flex gap-3">

                            href="#"
                            className="rounded-lg bg-foreground px-6 py-3 font-medium text-background hover:opacity-90"
                            >
                            Apply to teach
                        </a>

                        href="#"
                        className="rounded-lg border border-border px-6 py-3 font-medium hover:bg-secondary"
                        >
                        Learn more
                    </a>
                </div>
            </div>

            {/* Instructor dashboard preview */}
            <div className="rounded-2xl border border-border bg-secondary/50 p-6">
                <div className="mb-5 flex items-center gap-3">
                    <div className="grid h-10 w-10 place-items-center rounded-full bg-accent font-medium text-accent-foreground">
                        JD
                    </div>
                    <div>
                        <p className="text-sm font-medium">Jane Developer</p>
                        <p className="text-xs text-muted-foreground">12,484 students · 7 courses</p>
                    </div>
                </div>

                <div className="mb-5 grid grid-cols-2 gap-3">
                    <div className="rounded-lg bg-card p-3">
                        <p className="text-xs text-muted-foreground">This month</p>
                        <p className="font-heading text-xl font-bold">$3,240</p>
                        <p className="mt-1 text-xs text-success">↑ 18%</p>
                    </div>
                    <div className="rounded-lg bg-card p-3">
                        <p className="text-xs text-muted-foreground">New students</p>
                        <p className="font-heading text-xl font-bold">187</p>
                        <p className="mt-1 text-xs text-success">↑ 12%</p>
                    </div>
                </div>

                <div className="rounded-lg bg-card p-4">
                    <p className="mb-2 text-xs text-muted-foreground">Enrollments (last 14 days)</p>
                    <div className="flex h-16 items-end gap-1">
                        {enrollmentBars.map((h, i) => (
                            <div
                                key={i}
                                className={`flex-1 rounded-t ${i < 7 ? 'bg-primary/30' : 'bg-primary'}`}
                                style={{ height: `${h}%` }}
                            />
                        ))}
                    </div>
                </div>
            </div>
        </div>
</div>
</section>
);
}
