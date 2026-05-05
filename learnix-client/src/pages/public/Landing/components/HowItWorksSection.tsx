import { LANDING_PAGE } from '@/const/localization/landingPage';

const { HOW_IT_WORKS } = LANDING_PAGE;

export function HowItWorksSection() {
    return (
        <section className="py-20">
            <div className="mx-auto max-w-7xl px-6">
                <div className="mb-14 text-center">
                    <span className="text-sm font-semibold text-primary">{HOW_IT_WORKS.tag}</span>
                    <h2 className="mt-2 font-heading text-3xl font-bold md:text-4xl">
                        {HOW_IT_WORKS.heading}
                    </h2>
                    <p className="mx-auto mt-3 max-w-xl text-muted-foreground">
                        {HOW_IT_WORKS.subtitle}
                    </p>
                </div>

                <div className="relative grid gap-8 md:grid-cols-3">
                    <div className="absolute left-[16.67%] right-[16.67%] top-10 hidden h-px border-t-2 border-dashed border-border md:block" />

                    {HOW_IT_WORKS.steps.map((step) => (
                        <div key={step.n} className="relative text-center">
                            <div className="relative z-10 mx-auto grid h-20 w-20 place-items-center rounded-2xl border border-border bg-card shadow-md">
                                <span className="font-heading text-2xl font-bold text-primary">
                                    {step.n}
                                </span>
                            </div>
                            <h3 className="mt-5 font-heading text-xl font-semibold">
                                {step.title}
                            </h3>
                            <p className="mx-auto mt-3 max-w-xs text-sm text-muted-foreground">
                                {step.text}
                            </p>
                        </div>
                    ))}
                </div>
            </div>
        </section>
    );
}
