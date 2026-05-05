import { LANDING_PAGE } from '@/const/localization/landingPage';

const { TESTIMONIALS } = LANDING_PAGE;

export function TestimonialsSection() {
    return (
        <section className="py-20">
            <div className="mx-auto max-w-7xl px-6">
                <div className="mb-14 text-center">
                    <span className="text-sm font-semibold text-primary">{TESTIMONIALS.tag}</span>
                    <h2 className="mt-2 font-heading text-3xl font-bold md:text-4xl">
                        {TESTIMONIALS.heading}
                    </h2>
                    <p className="mt-3 text-muted-foreground">{TESTIMONIALS.subtitle}</p>
                </div>

                <div className="grid gap-6 md:grid-cols-3">
                    {TESTIMONIALS.items.map((t) => (
                        <div key={t.name} className="rounded-xl border border-border bg-card p-6">
                            <div className="mb-3 text-warning">
                                {'★'.repeat(t.rating)}
                                {'☆'.repeat(5 - t.rating)}
                            </div>
                            <p className="leading-relaxed text-foreground">"{t.text}"</p>
                            <div className="mt-5 flex items-center gap-3 border-t border-border pt-5">
                                <div
                                    className={`grid h-10 w-10 place-items-center rounded-full ${t.avatarBg} text-sm font-medium`}
                                >
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
