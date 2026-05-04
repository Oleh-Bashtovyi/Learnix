const stats = [
    { value: '1,200+', label: 'Courses across\n20+ categories' },
    { value: '85k',    label: 'Active learners\nworldwide' },
    { value: '4.8★',   label: 'Average course\nrating', highlightStar: true },
    { value: '24/7',   label: 'AI tutor\navailability' },
];

export function StatsSection() {
    return (
        <section className="border-y border-border bg-card">
            <div className="mx-auto grid max-w-7xl grid-cols-2 gap-8 px-6 py-10 text-center md:grid-cols-4">
                {stats.map((s) => (
                    <div key={s.value}>
                        <p className="font-heading text-4xl font-bold text-foreground md:text-5xl">
                            {s.highlightStar ? (
                                <>
                                    {s.value.replace('★', '')}
                                    <span className="text-warning">★</span>
                                </>
                            ) : (
                                s.value
                            )}
                        </p>
                        <p className="mt-2 whitespace-pre-line text-sm text-muted-foreground">{s.label}</p>
                    </div>
                ))}
            </div>
        </section>
    );
}