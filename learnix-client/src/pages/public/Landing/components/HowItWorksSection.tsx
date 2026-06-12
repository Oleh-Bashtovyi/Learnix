import { useTranslation } from 'react-i18next';

export function HowItWorksSection() {
    const { t } = useTranslation('landing');
    const steps = t('howItWorks.steps', { returnObjects: true }) as Array<{
        n: number;
        title: string;
        text: string;
    }>;

    return (
        <section className="pb-20 pt-10">
            <div className="mx-auto max-w-7xl px-6">
                <div className="mb-16 text-center">
                    <h2 className="font-heading text-3xl font-bold md:text-4xl">
                        {t('howItWorks.heading')}
                    </h2>
                    <p className="mx-auto mt-4 max-w-xl text-lg text-muted-foreground">
                        {t('howItWorks.subtitle')}
                    </p>
                </div>

                <div className="relative grid gap-12 md:grid-cols-3 md:gap-8">
                    <div className="absolute left-[16.67%] right-[16.67%] top-10 hidden h-[2px] bg-gradient-to-r from-transparent via-border to-transparent md:block" />

                    {steps.map((step) => (
                        <div key={step.n} className="group relative cursor-default text-center">
                            {/* Ambient glow behind the number */}
                            <div className="absolute left-1/2 top-10 -z-10 h-20 w-20 -translate-x-1/2 -translate-y-1/2 rounded-full bg-primary/10 blur-xl transition-all duration-500 group-hover:bg-primary/30 group-hover:blur-2xl" />

                            <div className="relative z-10 mx-auto grid h-20 w-20 place-items-center rounded-2xl border border-white/5 bg-card/60 shadow-lg backdrop-blur-xl transition-transform duration-500 group-hover:-translate-y-2 group-hover:shadow-primary/20">
                                <span className="bg-gradient-to-br from-primary to-accent bg-clip-text font-heading text-3xl font-extrabold text-transparent">
                                    {step.n}
                                </span>
                            </div>

                            <h3 className="mt-6 font-heading text-xl font-bold transition-colors group-hover:text-primary">
                                {step.title}
                            </h3>
                            <p className="mx-auto mt-3 max-w-[260px] text-sm leading-relaxed text-muted-foreground/90">
                                {step.text}
                            </p>
                        </div>
                    ))}
                </div>
            </div>
        </section>
    );
}
