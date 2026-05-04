const steps = [
    {
        n: 1,
        title: 'Pick your course',
        text: 'Browse 1,200+ courses by category, rating, or price. Try free lessons before you commit.',
    },
    {
        n: 2,
        title: 'Learn at your pace',
        text: 'Video, articles, quizzes. Stuck? The AI tutor is always one click away to explain anything.',
    },
    {
        n: 3,
        title: 'Earn your certificate',
        text: 'Complete the course → unlock a verifiable certificate. Share it on LinkedIn or your portfolio.',
    },
];

export function HowItWorksSection() {
    return (
        <section className="py-20">
            <div className="mx-auto max-w-7xl px-6">
                <div className="mb-14 text-center">
                    <span className="text-sm font-semibold text-primary">Process</span>
                    <h2 className="mt-2 font-heading text-3xl font-bold md:text-4xl">How Learnix works</h2>
                    <p className="mx-auto mt-3 max-w-xl text-muted-foreground">
                        From browsing to certification in three simple steps
                    </p>
                </div>

                <div className="relative grid gap-8 md:grid-cols-3">
                    <div className="absolute left-[16.67%] right-[16.67%] top-10 hidden h-px border-t-2 border-dashed border-border md:block" />

                    {steps.map((step) => (
                        <div key={step.n} className="relative text-center">
                            <div className="relative z-10 mx-auto grid h-20 w-20 place-items-center rounded-2xl border border-border bg-card shadow-md">
                                <span className="font-heading text-2xl font-bold text-primary">{step.n}</span>
                            </div>
                            <h3 className="mt-5 font-heading text-xl font-semibold">{step.title}</h3>
                            <p className="mx-auto mt-3 max-w-xs text-sm text-muted-foreground">{step.text}</p>
                        </div>
                    ))}
                </div>
            </div>
        </section>
    );
}