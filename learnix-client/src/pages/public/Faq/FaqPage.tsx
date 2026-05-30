import { FlaskConical, Search } from 'lucide-react';
import { FAQ_PAGE } from '@/const/localization/faqPage';
import { FaqSidebar } from './FaqSidebar';
import { FaqCategory } from './FaqCategory';

export default function FaqPage() {
    return (
        <div className="bg-background">
            {/* Pet-project disclaimer */}
            <div className="border-b border-warning/30 bg-warning/10">
                <div className="mx-auto flex max-w-7xl items-center gap-3 px-6 py-3 text-sm">
                    <FlaskConical className="h-4 w-4 shrink-0 text-warning" />
                    <span className="font-semibold text-warning">{FAQ_PAGE.DISCLAIMER.badge}:</span>
                    <span className="text-muted-foreground">{FAQ_PAGE.DISCLAIMER.text}</span>
                </div>
            </div>

            {/* Hero with search */}
            <div className="border-b border-border bg-gradient-to-b from-secondary/40 to-background">
                <div className="mx-auto max-w-3xl px-6 py-16 text-center">
                    <span className="inline-block rounded-full bg-accent/10 px-3 py-1.5 text-xs font-semibold uppercase tracking-wider text-accent">
                        {FAQ_PAGE.HERO.badge}
                    </span>
                    <h1 className="mt-4 font-heading text-4xl font-bold md:text-5xl">
                        {FAQ_PAGE.HERO.title}
                    </h1>
                    <p className="mt-3 text-lg text-muted-foreground">{FAQ_PAGE.HERO.subtitle}</p>

                    <div className="relative mx-auto mt-8 max-w-xl">
                        <Search className="absolute left-4 top-1/2 h-5 w-5 -translate-y-1/2 text-muted-foreground" />
                        <input
                            type="text"
                            placeholder={FAQ_PAGE.HERO.searchPlaceholder}
                            className="w-full rounded-xl border border-input bg-card py-4 pl-12 pr-4 text-base shadow-sm focus:outline-none focus:ring-2 focus:ring-ring"
                        />
                    </div>

                    {/* Popular searches */}
                    <div className="mt-5 flex flex-wrap justify-center gap-2 text-sm">
                        <span className="text-muted-foreground">{FAQ_PAGE.HERO.popular}</span>
                        {FAQ_PAGE.HERO.popularLinks.map((link, index) => (
                            <span key={index}>
                                <a href={link.anchor} className="text-primary hover:underline">
                                    {link.label}
                                </a>
                                {index < FAQ_PAGE.HERO.popularLinks.length - 1 && (
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
                    <FaqCategory category={FAQ_PAGE.CATEGORIES.GETTING_STARTED} isFirst />
                    <FaqCategory category={FAQ_PAGE.CATEGORIES.COURSES_AND_LEARNING} />
                    <FaqCategory category={FAQ_PAGE.CATEGORIES.PAYMENTS_AND_REFUNDS} />
                    <FaqCategory category={FAQ_PAGE.CATEGORIES.CERTIFICATES} />
                    <FaqCategory category={FAQ_PAGE.CATEGORIES.FOR_INSTRUCTORS} />
                    <FaqCategory category={FAQ_PAGE.CATEGORIES.AI_TUTOR} />
                    <FaqCategory category={FAQ_PAGE.CATEGORIES.ACCOUNT_AND_PRIVACY} />

                    {/* Still need help */}
                    <div className="mt-16 rounded-2xl border border-border bg-gradient-to-br from-primary/10 via-background to-accent/10 p-8 text-center md:p-10">
                        <div className="mx-auto grid h-14 w-14 place-items-center rounded-full border border-border bg-card text-2xl">
                            💬
                        </div>
                        <h3 className="mt-4 font-heading text-2xl font-bold">
                            {FAQ_PAGE.SUPPORT_SECTION.title}
                        </h3>
                        <p className="mt-2 text-muted-foreground">
                            {FAQ_PAGE.SUPPORT_SECTION.subtitle}
                        </p>
                        <div className="mt-6 flex flex-wrap justify-center gap-3">
                            <a
                                href="#"
                                className="rounded-lg bg-primary px-5 py-2.5 font-medium text-primary-foreground hover:bg-primary/90"
                            >
                                {FAQ_PAGE.SUPPORT_SECTION.contactCta}
                            </a>
                            <a
                                href="#"
                                className="rounded-lg border border-border bg-card px-5 py-2.5 font-medium hover:bg-secondary"
                            >
                                {FAQ_PAGE.SUPPORT_SECTION.discordCta}
                            </a>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    );
}
