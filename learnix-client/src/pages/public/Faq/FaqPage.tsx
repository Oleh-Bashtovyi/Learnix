import { Helmet } from 'react-helmet-async';
import { FlaskConical, Search } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { FaqSidebar } from './FaqSidebar';
import { FaqCategory } from './FaqCategory';
import { GitHubIcon } from '@/components/common/icons/SocialIcons';

const GITHUB_URL = 'https://github.com/Oleh-Bashtovyi/Learnix';

export default function FaqPage() {
    const { t } = useTranslation('faq');

    const gettingStarted = t('categories.gettingStarted', { returnObjects: true }) as object;
    const coursesAndLearning = t('categories.coursesAndLearning', {
        returnObjects: true,
    }) as object;
    const paymentsAndRefunds = t('categories.paymentsAndRefunds', {
        returnObjects: true,
    }) as object;
    const certificates = t('categories.certificates', { returnObjects: true }) as object;
    const forInstructors = t('categories.forInstructors', { returnObjects: true }) as object;
    const aiTutor = t('categories.aiTutor', { returnObjects: true }) as object;
    const accountAndPrivacy = t('categories.accountAndPrivacy', { returnObjects: true }) as object;

    return (
        <>
            <Helmet>
                <title>{t('seo.title')}</title>
                <meta name="description" content={t('seo.description')} />
                <meta property="og:title" content={t('seo.title')} />
                <meta property="og:description" content={t('seo.description')} />
            </Helmet>
            <div className="bg-background">
                {/* Pet-project disclaimer */}
                <div className="border-b border-warning/30 bg-warning/10">
                    <div className="mx-auto flex max-w-7xl items-start gap-3 px-6 py-3 text-sm md:items-center">
                        <FlaskConical className="mt-0.5 h-4 w-4 shrink-0 text-warning md:mt-0" />
                        <div className="flex flex-1 flex-col gap-2 md:flex-row md:items-center">
                            <div>
                                <span className="font-semibold text-warning">
                                    {t('disclaimer.badge')}:
                                </span>{' '}
                                <span className="text-muted-foreground">
                                    {t('disclaimer.text')}
                                </span>
                            </div>
                            <a
                                href={GITHUB_URL}
                                target="_blank"
                                rel="noopener noreferrer"
                                className="ml-auto flex shrink-0 items-center gap-1.5 font-medium text-warning hover:underline"
                            >
                                <GitHubIcon className="h-3.5 w-3.5" />
                                View source on GitHub ↗
                            </a>
                        </div>
                    </div>
                </div>

                {/* Hero with search */}
                <div className="border-b border-border bg-gradient-to-b from-secondary/40 to-background">
                    <div className="mx-auto max-w-3xl px-6 py-16 text-center">
                        <span className="inline-block rounded-full bg-accent/10 px-3 py-1.5 text-xs font-semibold uppercase tracking-wider text-accent">
                            {t('hero.badge')}
                        </span>
                        <h1 className="mt-4 font-heading text-4xl font-bold md:text-5xl">
                            {t('hero.title')}
                        </h1>
                        <p className="mt-3 text-lg text-muted-foreground">{t('hero.subtitle')}</p>

                        <div className="relative mx-auto mt-8 max-w-xl">
                            <Search className="absolute left-4 top-1/2 h-5 w-5 -translate-y-1/2 text-muted-foreground" />
                            <input
                                type="text"
                                placeholder={t('hero.searchPlaceholder')}
                                className="w-full rounded-xl border border-input bg-card py-4 pl-12 pr-4 text-base shadow-sm focus:outline-none focus:ring-2 focus:ring-ring"
                            />
                        </div>

                        {/* Popular searches */}
                        <div className="mt-5 flex flex-wrap justify-center gap-2 text-sm">
                            <span className="text-muted-foreground">{t('hero.popular')}</span>
                            {(
                                t('hero.popularLinks', { returnObjects: true }) as Array<{
                                    anchor: string;
                                    label: string;
                                }>
                            ).map((link, index) => (
                                <span key={index}>
                                    <a href={link.anchor} className="text-primary hover:underline">
                                        {link.label}
                                    </a>
                                    {index <
                                        (
                                            t('hero.popularLinks', {
                                                returnObjects: true,
                                            }) as unknown[]
                                        ).length -
                                            1 && (
                                        <span className="ml-2 text-muted-foreground">·</span>
                                    )}
                                </span>
                            ))}
                        </div>
                    </div>
                </div>

                {/* Two-column layout: sidebar nav + content */}
                <div className="mx-auto grid max-w-7xl gap-10 px-6 py-12 md:grid-cols-[240px_1fr]">
                    {/* Sidebar with category anchors */}
                    <FaqSidebar />

                    {/* Content */}
                    <div className="max-w-3xl space-y-12">
                        <FaqCategory
                            category={
                                gettingStarted as Parameters<typeof FaqCategory>[0]['category']
                            }
                            isFirst
                        />
                        <FaqCategory
                            category={
                                coursesAndLearning as Parameters<typeof FaqCategory>[0]['category']
                            }
                        />
                        <FaqCategory
                            category={
                                paymentsAndRefunds as Parameters<typeof FaqCategory>[0]['category']
                            }
                        />
                        <FaqCategory
                            category={certificates as Parameters<typeof FaqCategory>[0]['category']}
                        />
                        <FaqCategory
                            category={
                                forInstructors as Parameters<typeof FaqCategory>[0]['category']
                            }
                        />
                        <FaqCategory
                            category={aiTutor as Parameters<typeof FaqCategory>[0]['category']}
                        />
                        <FaqCategory
                            category={
                                accountAndPrivacy as Parameters<typeof FaqCategory>[0]['category']
                            }
                        />

                        {/* Still need help */}
                        <div className="mt-16 rounded-2xl border border-border bg-gradient-to-br from-primary/10 via-background to-accent/10 p-8 text-center md:p-10">
                            <div className="mx-auto grid h-14 w-14 place-items-center rounded-full border border-border bg-card text-2xl">
                                💬
                            </div>
                            <h3 className="mt-4 font-heading text-2xl font-bold">
                                {t('supportSection.title')}
                            </h3>
                            <p className="mt-2 text-muted-foreground">
                                {t('supportSection.subtitle')}
                            </p>
                            <div className="mt-6 flex flex-wrap justify-center gap-3">
                                <a
                                    href="#"
                                    className="rounded-lg bg-primary px-5 py-2.5 font-medium text-primary-foreground hover:bg-primary/90"
                                >
                                    {t('supportSection.contactCta')}
                                </a>
                                <a
                                    href="#"
                                    className="rounded-lg border border-border bg-card px-5 py-2.5 font-medium hover:bg-secondary"
                                >
                                    {t('supportSection.discordCta')}
                                </a>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </>
    );
}
