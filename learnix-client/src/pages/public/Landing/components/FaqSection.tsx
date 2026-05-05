import { LANDING_PAGE } from '@/const/localization/landingPage';

const { FAQ } = LANDING_PAGE;

export function FaqSection() {
    return (
        <section id="faq" className="py-20">
            <div className="mx-auto max-w-3xl px-6">
                <div className="mb-12 text-center">
                    <span className="text-sm font-semibold text-primary">{FAQ.tag}</span>
                    <h2 className="mt-2 font-heading text-3xl font-bold md:text-4xl">
                        {FAQ.heading}
                    </h2>
                    <p className="mt-3 text-muted-foreground">
                        {FAQ.subtitle}{' '}
                        <a href="#" className="text-primary hover:underline">
                            {FAQ.contactLabel}
                        </a>
                    </p>
                </div>

                <div className="space-y-3">
                    {FAQ.items.map((item, i) => (
                        <details
                            key={i}
                            open={item.defaultOpen}
                            className="group rounded-xl border border-border bg-card"
                        >
                            <summary className="flex cursor-pointer items-center justify-between rounded-xl p-5 hover:bg-secondary/50">
                                <span className="font-heading font-semibold">{item.q}</span>
                                <span className="faq-icon text-2xl font-light text-primary">+</span>
                            </summary>
                            <div className="px-5 pb-5 text-sm leading-relaxed text-muted-foreground">
                                {item.a}
                            </div>
                        </details>
                    ))}
                </div>

                <div className="mt-8 text-center">
                    <a href="#" className="font-medium text-primary hover:underline">
                        {FAQ.viewAll}
                    </a>
                </div>
            </div>
        </section>
    );
}
