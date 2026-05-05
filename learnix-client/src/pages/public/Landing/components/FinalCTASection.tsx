import { Link } from 'react-router-dom';
import { LANDING_PAGE } from '@/const/localization/landingPage';

const { FINAL_CTA } = LANDING_PAGE;

export function FinalCTASection() {
    return (
        <section className="py-20">
            <div className="mx-auto max-w-5xl px-6">
                <div className="relative overflow-hidden rounded-3xl bg-foreground p-12 text-center text-background md:p-16">
                    <div className="absolute -right-20 -top-20 h-64 w-64 rounded-full bg-primary/20 blur-3xl" />
                    <div className="absolute -bottom-20 -left-20 h-64 w-64 rounded-full bg-accent/20 blur-3xl" />

                    <div className="relative">
                        <h2 className="font-heading text-4xl font-bold md:text-5xl">
                            {FINAL_CTA.heading.line1}
                            <br />
                            {FINAL_CTA.heading.line2}
                        </h2>
                        <p className="mx-auto mt-5 max-w-xl text-lg text-background/70">
                            {FINAL_CTA.subtitle}
                        </p>
                        <div className="mt-8 flex flex-wrap justify-center gap-3">
                            <Link
                                to="/register"
                                className="rounded-lg bg-primary px-8 py-3.5 font-medium text-primary-foreground transition-colors hover:bg-primary/90"
                            >
                                {FINAL_CTA.cta.primary}
                            </Link>
                            <Link
                                to="/courses"
                                className="rounded-lg border border-background/30 px-8 py-3.5 font-medium text-background transition-colors hover:bg-background/10"
                            >
                                {FINAL_CTA.cta.secondary}
                            </Link>
                        </div>
                        <p className="mt-6 text-sm text-background/50">{FINAL_CTA.guarantees}</p>
                    </div>
                </div>
            </div>
        </section>
    );
}
