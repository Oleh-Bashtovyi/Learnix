import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import { motion } from 'framer-motion';
import type { Variants } from 'framer-motion';
import { BookOpen } from 'lucide-react';
import categoryFallback from '@/assets/categories/fallback.webp';
import { TextLink } from '@/components/common/elements/TextLink';
import { QueryError } from '@/components/common/system/QueryError';
import { useCategories } from '@/hooks/course/useCategories';
import { APP_ROUTES } from '@/routes/paths';
import { viewportConfig } from '@/utils/animations';

// Faster than the shared fadeUpVariant/staggerContainer — with 8 tiles in this grid,
// the default timing made the last cards visibly lag in on load.
const categoryFadeUpVariant: Variants = {
    initial: { opacity: 0, y: 16 },
    animate: {
        opacity: 1,
        y: 0,
        transition: { duration: 0.3, ease: [0.25, 0.1, 0.25, 1.0] },
    },
};

const categoryStaggerContainer: Variants = {
    initial: {},
    animate: {
        transition: {
            staggerChildren: 0.05,
            delayChildren: 0.05,
        },
    },
};

export function CategoriesSection() {
    const { t } = useTranslation('landing');
    const { data: categories = [], isLoading, isError, refetch: onRetry } = useCategories();

    // "Browse all categories" is an invitation, and an invitation only makes sense when there is somewhere to
    // go. With the list failing to load, the catalog behind that link is failing too — the last thing to do is
    // send the user into a second error.
    const hasContent = !isLoading && !isError && categories.length > 0;

    const renderContent = () => {
        if (isLoading) {
            return (
                <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
                    {Array.from({ length: 8 }).map((_, i) => (
                        <div
                            key={i}
                            className="h-[88px] animate-pulse rounded-2xl border border-white/5 bg-card/50"
                        />
                    ))}
                </div>
            );
        }

        if (isError) {
            return (
                <QueryError
                    message={t('categories.error')}
                    onRetry={onRetry}
                    retryLabel={t('common:actions.tryAgain')}
                    // A failed section must not hold the height of a full grid — it looks more broken than it is.
                    className="min-h-0 py-10"
                />
            );
        }

        if (categories.length === 0) {
            return (
                <div className="flex min-h-[200px] flex-col items-center justify-center gap-2 text-center">
                    <BookOpen className="size-10 text-muted-foreground/40" />
                    <p className="text-sm text-muted-foreground">{t('categories.empty')}</p>
                </div>
            );
        }

        return (
            <motion.div
                variants={categoryStaggerContainer}
                initial="initial"
                whileInView="animate"
                viewport={viewportConfig}
                className="grid grid-cols-2 gap-3 sm:grid-cols-2 sm:gap-5 lg:grid-cols-4"
            >
                {/* h-full on both the wrapper and the Link inside it: the grid stretches the wrapper
                    to the tallest card in the row, but the card the user sees is the Link, which
                    would otherwise stay as short as its own text — so a row holding a two-line
                    category name came out ragged. */}
                {categories.map((cat) => (
                    <motion.div variants={categoryFadeUpVariant} key={cat.id} className="h-full">
                        <Link
                            to={`/courses?categoryId=${cat.id}`}
                            className="group relative flex h-full flex-col items-center gap-2 overflow-hidden rounded-2xl border border-white/5 bg-gradient-to-b from-card/80 to-card/40 p-3 text-center backdrop-blur-xl transition-all duration-500 hover:-translate-y-1 hover:border-primary/30 hover:shadow-[0_10px_40px_-10px_rgba(var(--primary),0.3)] sm:flex-row sm:items-center sm:gap-4 sm:p-4 sm:text-left"
                        >
                            {/* Hover glow effect */}
                            <div className="absolute -right-8 -top-8 size-32 rounded-full bg-primary/10 opacity-0 blur-[30px] transition-opacity duration-500 group-hover:opacity-100" />

                            {/* Icon — whatever the backend has for this category, or the bundled
                                placeholder. A category the admin has not given an image to still
                                gets a picture, and a category the admin invents tomorrow needs no
                                code change: the slug→emoji table this used to consult only knew
                                the slugs someone had thought to hardcode. */}
                            <div className="relative z-10 shrink-0">
                                <img
                                    src={cat.imageUrl ?? categoryFallback}
                                    alt=""
                                    className="size-10 rounded-xl object-cover shadow-sm transition-transform duration-500 group-hover:scale-110 sm:size-14"
                                />
                            </div>

                            {/* Content */}
                            <div className="relative z-10 flex min-w-0 flex-1 flex-col py-1">
                                {/* Wraps rather than truncates: "Web Development" and "Language
                                    Learning" are real category names, and a name clipped to
                                    "Web Developm…" is worse than a name on two lines. The grid
                                    equalises the row height, so a two-line card costs nothing. */}
                                {/* Kept at 16px: a category tile is a smaller thing than a course card,
                                    whose own title is text-lg — going bigger here made the tile shout
                                    louder than the courses it points at. */}
                                <h3 className="line-clamp-2 font-heading text-[15px] font-bold leading-tight text-foreground/90 transition-colors group-hover:text-foreground sm:text-base">
                                    {cat.name}
                                </h3>
                                {/* A pluralised sentence, not a number glued to the navigation menu's
                                    label — that read "1 Курси" in Ukrainian, where the noun has to
                                    decline with the count. */}
                                <p className="mt-1 truncate text-xs font-medium text-muted-foreground/70 transition-colors group-hover:text-muted-foreground sm:text-sm">
                                    {t('categories.courseCount', { count: cat.coursesCount })}
                                </p>
                            </div>

                            {/* Animated Arrow */}
                            <div className="relative z-10 hidden size-8 shrink-0 -translate-x-2 place-items-center rounded-full bg-primary/10 text-primary opacity-0 transition-all duration-300 group-hover:translate-x-0 group-hover:opacity-100 sm:grid">
                                <svg
                                    className="size-4"
                                    fill="none"
                                    viewBox="0 0 24 24"
                                    stroke="currentColor"
                                >
                                    <path
                                        strokeLinecap="round"
                                        strokeLinejoin="round"
                                        strokeWidth={2}
                                        d="M9 5l7 7-7 7"
                                    />
                                </svg>
                            </div>
                        </Link>
                    </motion.div>
                ))}
            </motion.div>
        );
    };

    // The stats section above already ends on 40px of its own padding, so the pt-20 this section used
    // to carry stacked into a 120px void between it and the heading below.
    return (
        <section id="categories" className="pb-12 pt-10 sm:pt-12">
            <div className="mx-auto max-w-7xl px-6">
                <div className="mb-6 flex items-end justify-between sm:mb-8">
                    <div>
                        <h2 className="font-heading text-3xl font-bold md:text-4xl">
                            {t('categories.heading')}
                        </h2>
                    </div>
                    {/* One link per breakpoint: this one on desktop, the one below the grid on mobile. */}
                    {hasContent && (
                        <TextLink
                            to={APP_ROUTES.public.courses}
                            className="hidden text-sm md:inline"
                        >
                            {t('categories.viewAll')}
                        </TextLink>
                    )}
                </div>

                {renderContent()}

                {hasContent && (
                    <div className="mt-8 flex justify-center md:hidden">
                        <TextLink to={APP_ROUTES.public.courses} className="text-sm">
                            {t('categories.viewAll')}
                        </TextLink>
                    </div>
                )}
            </div>
        </section>
    );
}
