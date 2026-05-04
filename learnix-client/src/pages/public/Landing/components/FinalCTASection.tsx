import { Link } from 'react-router-dom';

export function FinalCTASection() {
    return (
        <section className="py-20">
            <div className="mx-auto max-w-5xl px-6">
                <div className="relative overflow-hidden rounded-3xl bg-foreground p-12 text-center text-background md:p-16">
                    <div className="absolute -right-20 -top-20 h-64 w-64 rounded-full bg-primary/20 blur-3xl" />
                    <div className="absolute -bottom-20 -left-20 h-64 w-64 rounded-full bg-accent/20 blur-3xl" />

                    <div className="relative">
                        <h2 className="font-heading text-4xl font-bold md:text-5xl">
                            Ready to learn something
                            <br />
                            you'll actually use?
                        </h2>
                        <p className="mx-auto mt-5 max-w-xl text-lg text-background/70">
                            Join 85,000+ learners. Start with a free course today — no credit card required.
                        </p>
                        <div className="mt-8 flex flex-wrap justify-center gap-3">
                            <Link
                                to="/register"
                                className="rounded-lg bg-primary px-8 py-3.5 font-medium text-primary-foreground transition-colors hover:bg-primary/90"
                            >
                                Get started free
                            </Link>
                            <Link
                                to="/courses"
                                className="rounded-lg border border-background/30 px-8 py-3.5 font-medium text-background transition-colors hover:bg-background/10"
                            >
                                Browse courses
                            </Link>
                        </div>
                        <p className="mt-6 text-sm text-background/50">
                            ✓ Free forever for free courses · ✓ 30-day refund on paid · ✓ Cancel anytime
                        </p>
                    </div>
                </div>
            </div>
        </section>
    );
}