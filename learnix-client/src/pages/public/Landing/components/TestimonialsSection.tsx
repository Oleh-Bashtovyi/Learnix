interface Testimonial {
    initials: string;
    name: string;
    role: string;
    rating: 4 | 5;
    text: string;
    avatarBg: string;
}

const testimonials: Testimonial[] = [
    {
        initials: 'MK',
        name: 'Maksym K.',
        role: 'Frontend Engineer · Kyiv',
        rating: 5,
        text: "Best React course I've taken. Jane shows real-world code, not toy examples. The performance module alone is worth the price.",
        avatarBg: 'bg-primary/20',
    },
    {
        initials: 'SJ',
        name: 'Sarah Johnson',
        role: 'Self-taught dev · Berlin',
        rating: 5,
        text: "The AI tutor is a game changer. I no longer feel stuck. It explains things in ways tutorials never did, and it knows where I'm in the course.",
        avatarBg: 'bg-accent/20',
    },
    {
        initials: 'RT',
        name: 'Rohan T.',
        role: 'Junior Developer · Bengaluru',
        rating: 5,
        text: "Got my first dev job after finishing two Learnix courses and shipping the projects to GitHub. The certificates actually got recruiters' attention.",
        avatarBg: 'bg-warning/20',
    },
    {
        initials: 'LO',
        name: 'Léa Okafor',
        role: 'Product Designer · Paris',
        rating: 5,
        text: 'Finally a platform that respects my time. Clear curriculum, no fluff, no hour-long intros. I can binge a section on a lunch break.',
        avatarBg: 'bg-success/20',
    },
    {
        initials: 'DC',
        name: 'David Chen',
        role: 'Instructor · San Francisco',
        rating: 5,
        text: "As an instructor, the dashboard is clean and the editor doesn't get in my way. I shipped my first course in a weekend.",
        avatarBg: 'bg-primary/20',
    },
    {
        initials: 'AN',
        name: 'Aiko Nakamura',
        role: 'Full-stack dev · Tokyo',
        rating: 4,
        text: "Course quality varies (as it does everywhere) but ratings make it easy to find the gems. I've completed 12 courses and only one was meh.",
        avatarBg: 'bg-accent/20',
    },
];

export function TestimonialsSection() {
    return (
        <section className="py-20">
            <div className="mx-auto max-w-7xl px-6">
                <div className="mb-14 text-center">
                    <span className="text-sm font-semibold text-primary">Reviews</span>
                    <h2 className="mt-2 font-heading text-3xl font-bold md:text-4xl">Loved by learners</h2>
                    <p className="mt-3 text-muted-foreground">
                        85,000+ students, 4.8 average rating across 1,200+ courses
                    </p>
                </div>

                <div className="grid gap-6 md:grid-cols-3">
                    {testimonials.map((t) => (
                        <div key={t.name} className="rounded-xl border border-border bg-card p-6">
                            <div className="mb-3 text-warning">
                                {'★'.repeat(t.rating)}
                                {'☆'.repeat(5 - t.rating)}
                            </div>
                            <p className="leading-relaxed text-foreground">"{t.text}"</p>
                            <div className="mt-5 flex items-center gap-3 border-t border-border pt-5">
                                <div className={`grid h-10 w-10 place-items-center rounded-full ${t.avatarBg} text-sm font-medium`}>
                                    {t.initials}
                                </div>
                                <div>
                                    <p className="text-sm font-medium">{t.name}</p>
                                    <p className="text-xs text-muted-foreground">{t.role}</p>
                                </div>
                            </div>
                        </div>
                    ))}
                </div>
            </div>
        </section>
    );
}
