interface FaqItem {
    q: string;
    a: string;
    defaultOpen?: boolean;
}

const faqItems: FaqItem[] = [
    {
        q: 'How does Learnix differ from other LMS platforms?',
        a: 'Three things: (1) Every course has an AI tutor that knows your progress and lesson context, available 24/7. (2) We focus on project-based learning — you finish with shippable artifacts, not just notes. (3) Our instructors keep 85% of revenue, so they\'re motivated to deliver real value.',
        defaultOpen: true,
    },
    {
        q: 'Are courses one-time purchase or subscription?',
        a: 'One-time purchase — you own a course forever once you buy it. There are no recurring fees, no expiring access. Free courses stay free.',
    },
    {
        q: 'What if I don\'t like a course after buying?',
        a: 'Every paid course offers a free preview lesson, and we have a 30-day refund policy if you\'ve completed less than 50% of the course. No questions, no friction.',
    },
    {
        q: 'Does the AI tutor cost extra?',
        a: 'No. Unlimited AI tutor usage is included with every course you own — both free and paid. Conversation history is saved across sessions and devices.',
    },
];

export function FaqSection() {
    return (
        <section id="faq" className="py-20">
            <div className="mx-auto max-w-3xl px-6">
                <div className="mb-12 text-center">
                    <span className="text-sm font-semibold text-primary">FAQ</span>
                    <h2 className="mt-2 font-heading text-3xl font-bold md:text-4xl">Common questions</h2>
                    <p className="mt-3 text-muted-foreground">
                        Can't find what you need?{' '}
                        <a href="#" className="text-primary hover:underline">
                            Contact support
                        </a>
                    </p>
                </div>

                <div className="space-y-3">
                    {faqItems.map((item, i) => (
                        <details
                            key={i}
                            open={item.defaultOpen}
                            className="group rounded-xl border border-border bg-card"
                        >
                            <summary className="flex cursor-pointer items-center justify-between rounded-xl p-5 hover:bg-secondary/50">
                                <span className="font-heading font-semibold">{item.q}</span>
                                <span className="faq-icon text-2xl font-light text-primary">+</span>
                            </summary>
                            <div className="px-5 pb-5 text-sm leading-relaxed text-muted-foreground">{item.a}</div>
                        </details>
                    ))}
                </div>

                <div className="mt-8 text-center">
                    <a href="#" className="font-medium text-primary hover:underline">
                        View all questions →
                    </a>
                </div>
            </div>
        </section>
    );
}
