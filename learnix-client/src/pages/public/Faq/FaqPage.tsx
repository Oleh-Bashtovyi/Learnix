import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Mail } from 'lucide-react';
import { Seo } from '@/components/common/seo/Seo';
import { ProjectNoticeBanner } from '@/components/common/ui/ProjectNoticeBanner';
import { SearchInput } from '@/components/common/ui/SearchInput';
import { EXTERNAL_LINKS } from '@/const/links.constants';
import { usePublicConfig } from '@/hooks/shared/usePublicConfig';
import { cn } from '@/utils/cn';
import { faqJsonLd } from '@/utils/seo';
import { FaqCategory } from './FaqCategory';
import { type FaqSearchHit, FaqSearchResults } from './FaqSearchResults';
import { FaqSidebar } from './FaqSidebar';

interface FaqItem {
    q: string;
    a: string;
}

export default function FaqPage() {
    const { t } = useTranslation('faq');

    const { data: config } = usePublicConfig();
    const provider = config?.aiProvider || 'AI';

    const gettingStarted = t('categories.gettingStarted', { returnObjects: true }) as object;
    const coursesAndLearning = t('categories.coursesAndLearning', {
        returnObjects: true,
    }) as object;
    const paymentsAndRefunds = t('categories.paymentsAndRefunds', {
        returnObjects: true,
    }) as object;
    const certificates = t('categories.certificates', { returnObjects: true }) as object;
    const forInstructors = t('categories.forInstructors', { returnObjects: true }) as object;
    const aiTutor = t('categories.aiTutor', {
        returnObjects: true,
        aiProvider: provider,
    }) as object;
    const accountAndPrivacy = t('categories.accountAndPrivacy', { returnObjects: true }) as object;

    const allCategories = [
        gettingStarted,
        coursesAndLearning,
        paymentsAndRefunds,
        certificates,
        forInstructors,
        aiTutor,
        accountAndPrivacy,
    ] as { items: FaqItem[] }[];
    const allQuestions = allCategories.flatMap((category) => category.items);

    // The search box was decoration — a `<SearchInput>` with no value and no handler. It looked like a
    // control and did nothing, which is worse than not having one. Every answer is already here, in
    // memory, so searching them is a filter, not a request: no debounce, no endpoint, no loading state.
    const [query, setQuery] = useState('');
    const trimmedQuery = query.trim();

    const hits: FaqSearchHit[] = trimmedQuery
        ? allCategories.flatMap((category) => {
              const titled = category as unknown as { title: string; items: FaqItem[] };
              const needle = trimmedQuery.toLowerCase();

              // The question and the answer both count: people search for a word they remember from an
              // answer at least as often as for the question it answers.
              return (titled.items ?? [])
                  .filter(
                      (item) =>
                          item.q.toLowerCase().includes(needle) ||
                          item.a.toLowerCase().includes(needle),
                  )
                  .map((item) => ({ categoryTitle: titled.title, q: item.q, a: item.a }));
          })
        : [];

    return (
        <>
            <Seo
                title={t('seo.title')}
                description={t('seo.description')}
                jsonLd={faqJsonLd(allQuestions)}
            />
            <div className="bg-background">
                <ProjectNoticeBanner />

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

                        <div className="mx-auto mt-8 max-w-xl">
                            <SearchInput
                                value={query}
                                onChange={(e) => setQuery(e.target.value)}
                                onClear={() => setQuery('')}
                                placeholder={t('hero.searchPlaceholder')}
                                className="rounded-xl py-4 pl-11 text-base"
                            />
                        </div>

                        {/* Popular searches. Hidden while searching: they are anchors into categories
                            that are not on the page at that moment, so they would scroll to nothing. */}
                        <div
                            className={cn(
                                'mt-5 flex-wrap justify-center gap-2 text-sm',
                                trimmedQuery ? 'hidden' : 'flex',
                            )}
                        >
                            <span className="text-muted-foreground">{t('hero.popular')}</span>
                            {(
                                t('hero.popularLinks', { returnObjects: true }) as Array<{
                                    anchor: string;
                                    label: string;
                                }>
                            ).map((link, index) => (
                                <span key={index}>
                                    <a href={link.anchor} className="text-link hover:underline">
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

                {/* Two-column layout: sidebar nav + content. While searching there is one column and no
                    sidebar — its links are anchors into categories the search has just replaced. */}
                <div
                    className={cn(
                        'mx-auto grid max-w-7xl gap-10 px-6 py-12',
                        !trimmedQuery && 'md:grid-cols-[240px_1fr]',
                    )}
                >
                    {!trimmedQuery && <FaqSidebar />}

                    {/* Content */}
                    <div
                        className={cn(
                            'min-w-0 max-w-3xl space-y-12',
                            trimmedQuery && 'mx-auto w-full',
                        )}
                    >
                        {trimmedQuery ? (
                            <FaqSearchResults
                                query={trimmedQuery}
                                hits={hits}
                                onClear={() => setQuery('')}
                            />
                        ) : (
                            <>
                                <FaqCategory
                                    category={
                                        gettingStarted as Parameters<
                                            typeof FaqCategory
                                        >[0]['category']
                                    }
                                    isFirst
                                />
                                <FaqCategory
                                    category={
                                        coursesAndLearning as Parameters<
                                            typeof FaqCategory
                                        >[0]['category']
                                    }
                                />
                                <FaqCategory
                                    category={
                                        paymentsAndRefunds as Parameters<
                                            typeof FaqCategory
                                        >[0]['category']
                                    }
                                />
                                <FaqCategory
                                    category={
                                        certificates as Parameters<
                                            typeof FaqCategory
                                        >[0]['category']
                                    }
                                />
                                <FaqCategory
                                    category={
                                        forInstructors as Parameters<
                                            typeof FaqCategory
                                        >[0]['category']
                                    }
                                />
                                <FaqCategory
                                    category={
                                        aiTutor as Parameters<typeof FaqCategory>[0]['category']
                                    }
                                />
                                <FaqCategory
                                    category={
                                        accountAndPrivacy as Parameters<
                                            typeof FaqCategory
                                        >[0]['category']
                                    }
                                />
                            </>
                        )}

                        {/* Still need help — the footer's "Contact" link targets this id. */}
                        <div
                            id="support"
                            className="mt-16 scroll-mt-24 rounded-2xl border border-border bg-gradient-to-br from-primary/10 via-background to-accent/10 p-8 text-center md:p-10"
                        >
                            <div className="mx-auto grid size-14 place-items-center rounded-full border border-border bg-card text-2xl">
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
                                    href={EXTERNAL_LINKS.supportMailto}
                                    className="flex items-center gap-2 rounded-lg bg-primary px-5 py-2.5 font-medium text-primary-foreground hover:bg-primary/90"
                                >
                                    <Mail className="size-4" />
                                    {t('supportSection.contactCta')}
                                </a>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </>
    );
}
