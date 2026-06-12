import { useTranslation } from 'react-i18next';

export function FaqSidebar() {
    const { t } = useTranslation('faq');
    const topics = t('topics', { returnObjects: true }) as {
        heading: string;
        categories: Array<{ id: string; label: string }>;
        supportTitle: string;
        supportSubtitle: string;
        supportCta: string;
    };

    return (
        <aside className="md:sticky md:top-20 md:self-start">
            <p className="mb-3 text-xs font-semibold uppercase tracking-wider text-muted-foreground">
                {topics.heading}
            </p>
            <nav className="flex gap-1 overflow-x-auto md:flex-col md:overflow-visible">
                {topics.categories.map((category, index) => (
                    <a
                        key={category.id}
                        href={`#${category.id}`}
                        className={`whitespace-nowrap rounded-lg px-3 py-2 text-sm ${
                            index === 0
                                ? 'bg-primary/10 font-medium text-primary'
                                : 'text-muted-foreground hover:bg-secondary hover:text-foreground'
                        }`}
                    >
                        {category.label}
                    </a>
                ))}
            </nav>

            <div className="mt-8 hidden rounded-xl border border-border bg-card p-4 md:block">
                <p className="text-sm font-medium">{topics.supportTitle}</p>
                <p className="mt-1 text-xs text-muted-foreground">{topics.supportSubtitle}</p>
                <a href="#" className="mt-3 inline-block text-sm text-primary hover:underline">
                    {topics.supportCta}
                </a>
            </div>
        </aside>
    );
}
