import { useTranslation } from 'react-i18next';

interface StackCardProps {
    /** A key under `about.stack`, each holding a `title` and an `items` array. */
    groupKey: 'backend' | 'frontend' | 'platform';
}

export function StackCard({ groupKey }: StackCardProps) {
    const { t } = useTranslation('about');
    const items = t(`stack.${groupKey}.items`, { returnObjects: true }) as string[];

    return (
        <div className="rounded-xl border border-border bg-card p-5">
            <h3 className="font-heading text-sm font-semibold uppercase tracking-wider text-primary">
                {t(`stack.${groupKey}.title`)}
            </h3>
            <ul className="mt-4 space-y-2.5 text-sm text-muted-foreground">
                {items.map((item) => (
                    <li key={item} className="flex gap-2.5">
                        <span
                            className="mt-2 size-1.5 shrink-0 rounded-full bg-primary/50"
                            aria-hidden="true"
                        />
                        <span className="leading-relaxed">{item}</span>
                    </li>
                ))}
            </ul>
        </div>
    );
}
