import { useTranslation } from 'react-i18next';
import { TextLink } from '@/components/common/ui/TextLink';
import { APP_ROUTES } from '@/routes/paths';

export function FaqSection() {
    const { t } = useTranslation('landing');
    const items = t('faq.items', { returnObjects: true }) as Array<{
        q: string;
        a: string;
        defaultOpen: boolean;
    }>;

    return (
        <section id="faq" className="pb-20 pt-10">
            <div className="mx-auto max-w-3xl px-6">
                <div className="mb-14 text-center">
                    <h2 className="font-heading text-3xl font-bold md:text-4xl">
                        {t('faq.heading')}
                    </h2>
                    <p className="mt-3 text-muted-foreground">
                        {t('faq.subtitle')}{' '}
                        <TextLink to={APP_ROUTES.public.faqSupport}>
                            {t('faq.contactLabel')}
                        </TextLink>
                    </p>
                </div>

                <div className="space-y-3">
                    {items.map((item, i) => (
                        <details
                            key={i}
                            open={item.defaultOpen}
                            className="group relative overflow-hidden rounded-2xl border border-border/50 bg-card/40 shadow-sm backdrop-blur-sm transition-all hover:border-primary/30"
                        >
                            <summary className="flex cursor-pointer items-center justify-between p-6 transition-colors hover:bg-secondary/20">
                                <span className="pr-6 font-heading text-[15px] font-semibold text-foreground/90 transition-colors group-hover:text-primary">
                                    {item.q}
                                </span>
                                <span className="faq-icon grid size-8 shrink-0 place-items-center rounded-full bg-primary/10 text-xl font-light text-primary transition-all group-hover:bg-primary/20">
                                    +
                                </span>
                            </summary>
                            <div className="px-6 pb-6 text-[14px] leading-relaxed text-muted-foreground/90">
                                {item.a}
                            </div>
                        </details>
                    ))}
                </div>

                <div className="mt-8 text-center">
                    <TextLink to={APP_ROUTES.public.faq}>{t('faq.viewAll')}</TextLink>
                </div>
            </div>
        </section>
    );
}
