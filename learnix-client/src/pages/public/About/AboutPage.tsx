import { Helmet } from 'react-helmet-async';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import { Check, X } from 'lucide-react';
import { GitHubIcon } from '@/components/common/icons/SocialIcons';
import { EXTERNAL_LINKS } from '@/const/links.constants';
import { APP_ROUTES } from '@/routes/paths';
import { StackCard } from './components/StackCard';

interface PrivacyItem {
    title: string;
    body: string;
}

export default function AboutPage() {
    const { t } = useTranslation('about');

    const privacyItems = t('privacy.items', { returnObjects: true }) as PrivacyItem[];
    const realItems = t('honest.real', { returnObjects: true }) as string[];
    const mockedItems = t('honest.mocked', { returnObjects: true }) as string[];

    return (
        <>
            <Helmet>
                <title>{t('seo.title')}</title>
                <meta name="description" content={t('seo.description')} />
                <meta property="og:title" content={t('seo.title')} />
                <meta property="og:description" content={t('seo.description')} />
            </Helmet>

            <div className="bg-background">
                <div className="border-b border-border bg-gradient-to-b from-secondary/40 to-background">
                    <div className="mx-auto max-w-3xl px-6 py-16 text-center">
                        <span className="inline-block rounded-full bg-accent/10 px-3 py-1.5 text-xs font-semibold uppercase tracking-wider text-accent">
                            {t('hero.badge')}
                        </span>
                        <h1 className="mt-4 font-heading text-4xl font-bold md:text-5xl">
                            {t('hero.title')}
                        </h1>
                        <p className="mt-4 text-lg leading-relaxed text-muted-foreground">
                            {t('hero.subtitle')}
                        </p>

                        <div className="mt-8 flex flex-wrap justify-center gap-3">
                            <a
                                href={EXTERNAL_LINKS.githubRepo}
                                target="_blank"
                                rel="noopener noreferrer"
                                className="flex items-center gap-2 rounded-lg bg-primary px-5 py-2.5 font-medium text-primary-foreground hover:bg-primary/90"
                            >
                                <GitHubIcon className="size-4" />
                                {t('hero.sourceCta')}
                            </a>
                            <Link
                                to={APP_ROUTES.public.courses}
                                className="rounded-lg border border-border bg-card px-5 py-2.5 font-medium hover:bg-secondary"
                            >
                                {t('hero.browseCta')}
                            </Link>
                        </div>
                    </div>
                </div>

                <div className="mx-auto max-w-3xl space-y-16 px-6 py-16">
                    <section>
                        <h2 className="font-heading text-2xl font-bold">{t('what.title')}</h2>
                        <p className="mt-4 leading-relaxed text-muted-foreground">
                            {t('what.body')}
                        </p>
                        <p className="mt-4 rounded-xl border border-border bg-card p-4 text-sm leading-relaxed text-muted-foreground">
                            {t('what.aiNote')}
                        </p>
                    </section>

                    <section>
                        <h2 className="font-heading text-2xl font-bold">{t('why.title')}</h2>
                        <p className="mt-4 leading-relaxed text-muted-foreground">
                            {t('why.body')}
                        </p>
                    </section>

                    <section>
                        <h2 className="font-heading text-2xl font-bold">{t('stack.title')}</h2>
                        <div className="mt-6 space-y-4">
                            <StackCard groupKey="backend" />
                            <StackCard groupKey="frontend" />
                            <StackCard groupKey="platform" />
                        </div>
                    </section>

                    <section>
                        <h2 className="font-heading text-2xl font-bold">{t('honest.title')}</h2>
                        <div className="mt-6 grid gap-4 md:grid-cols-2">
                            <div className="rounded-xl border border-success/30 bg-success/5 p-5">
                                <h3 className="font-heading text-sm font-semibold uppercase tracking-wider text-success">
                                    {t('honest.realTitle')}
                                </h3>
                                <ul className="mt-4 space-y-3 text-sm text-muted-foreground">
                                    {realItems.map((item) => (
                                        <li key={item} className="flex gap-2.5">
                                            <Check
                                                className="mt-0.5 size-4 shrink-0 text-success"
                                                aria-hidden="true"
                                            />
                                            <span className="leading-relaxed">{item}</span>
                                        </li>
                                    ))}
                                </ul>
                            </div>

                            <div className="rounded-xl border border-warning/30 bg-warning/5 p-5">
                                <h3 className="font-heading text-sm font-semibold uppercase tracking-wider text-warning">
                                    {t('honest.mockedTitle')}
                                </h3>
                                <ul className="mt-4 space-y-3 text-sm text-muted-foreground">
                                    {mockedItems.map((item) => (
                                        <li key={item} className="flex gap-2.5">
                                            <X
                                                className="mt-0.5 size-4 shrink-0 text-warning"
                                                aria-hidden="true"
                                            />
                                            <span className="leading-relaxed">{item}</span>
                                        </li>
                                    ))}
                                </ul>
                            </div>
                        </div>
                    </section>

                    {/* The footer's only legal link lands here — keep the id stable. */}
                    <section id="privacy" className="scroll-mt-24">
                        <h2 className="font-heading text-2xl font-bold">{t('privacy.title')}</h2>
                        <p className="mt-4 leading-relaxed text-muted-foreground">
                            {t('privacy.intro')}
                        </p>
                        <dl className="mt-6 space-y-5">
                            {privacyItems.map((item) => (
                                <div
                                    key={item.title}
                                    className="rounded-xl border border-border bg-card p-5"
                                >
                                    <dt className="font-heading font-semibold text-foreground">
                                        {item.title}
                                    </dt>
                                    <dd className="mt-2 text-sm leading-relaxed text-muted-foreground">
                                        {item.body}
                                    </dd>
                                </div>
                            ))}
                        </dl>
                    </section>

                    <section className="rounded-2xl border border-border bg-gradient-to-br from-primary/10 via-background to-accent/10 p-8 text-center md:p-10">
                        <h2 className="font-heading text-2xl font-bold">{t('links.title')}</h2>
                        <p className="mt-2 text-muted-foreground">{t('links.subtitle')}</p>
                        <div className="mt-6 flex flex-wrap justify-center gap-3">
                            <a
                                href={EXTERNAL_LINKS.githubRepo}
                                target="_blank"
                                rel="noopener noreferrer"
                                className="flex items-center gap-2 rounded-lg bg-primary px-5 py-2.5 font-medium text-primary-foreground hover:bg-primary/90"
                            >
                                <GitHubIcon className="size-4" />
                                {t('links.repoCta')}
                            </a>
                            <Link
                                to={APP_ROUTES.public.faq}
                                className="rounded-lg border border-border bg-card px-5 py-2.5 font-medium hover:bg-secondary"
                            >
                                {t('links.faqCta')}
                            </Link>
                        </div>
                    </section>
                </div>
            </div>
        </>
    );
}
