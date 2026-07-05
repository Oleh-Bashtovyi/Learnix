import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import { motion } from 'framer-motion';
import { BookOpen } from 'lucide-react';
import { QueryError } from '@/components/common/system/QueryError';
import { APP_ROUTES } from '@/routes/paths';
import { fadeUpVariant, staggerContainer, viewportConfig } from '@/utils/animations';
import { cn } from '@/utils/cn';
import type { LandingCategory } from '@/utils/mocks/landing.mock';

interface CategoriesSectionProps {
    categories: LandingCategory[];
    isLoading?: boolean;
    isError?: boolean;
    onRetry?: () => void;
}

export function CategoriesSection({
    categories,
    isLoading,
    isError,
    onRetry,
}: CategoriesSectionProps) {
    const { t } = useTranslation('landing');

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
                    retryLabel={t('categories.retry')}
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
                variants={staggerContainer}
                initial="initial"
                whileInView="animate"
                viewport={viewportConfig}
                className="grid grid-cols-2 gap-3 sm:grid-cols-2 sm:gap-5 lg:grid-cols-4"
            >
                {categories.map((cat) => (
                    <motion.div variants={fadeUpVariant} key={cat.id}>
                        <Link
                            to={`/courses?categoryId=${cat.id}`}
                            className="group relative flex flex-col items-center gap-2 overflow-hidden rounded-2xl border border-white/5 bg-gradient-to-b from-card/80 to-card/40 p-3 text-center backdrop-blur-xl transition-all duration-500 hover:-translate-y-1 hover:border-primary/30 hover:shadow-[0_10px_40px_-10px_rgba(var(--primary),0.3)] sm:flex-row sm:items-start sm:gap-4 sm:p-4 sm:text-left"
                        >
                            {/* Hover glow effect */}
                            <div className="absolute -right-8 -top-8 size-32 rounded-full bg-primary/10 opacity-0 blur-[30px] transition-opacity duration-500 group-hover:opacity-100" />

                            {/* Icon */}
                            <div className="relative z-10 shrink-0">
                                {cat.imageUrl ? (
                                    <img
                                        src={cat.imageUrl}
                                        alt=""
                                        className="size-10 rounded-xl object-cover shadow-sm transition-transform duration-500 group-hover:scale-110 sm:size-14"
                                    />
                                ) : (
                                    <div
                                        className={cn(
                                            'grid size-10 place-items-center rounded-xl text-xl shadow-sm transition-transform duration-500 group-hover:scale-110 sm:size-14 sm:text-2xl',
                                            cat.iconBgClass,
                                            cat.iconTextClass,
                                        )}
                                    >
                                        {cat.emoji}
                                    </div>
                                )}
                            </div>

                            {/* Content */}
                            <div className="relative z-10 flex min-w-0 flex-1 flex-col py-1">
                                <h3 className="truncate font-heading text-[15px] font-bold text-foreground/90 transition-colors group-hover:text-foreground sm:text-base">
                                    {cat.name}
                                </h3>
                                <p className="mt-0.5 truncate text-xs font-medium text-muted-foreground/70 transition-colors group-hover:text-muted-foreground">
                                    {cat.coursesCount} {t('categories.coursesLabel')}
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

    return (
        <section id="categories" className="pb-12 pt-20">
            <div className="mx-auto max-w-7xl px-6">
                <div className="mb-10 flex items-end justify-between">
                    <div>
                        <h2 className="font-heading text-3xl font-bold md:text-4xl">
                            {t('categories.heading')}
                        </h2>
                    </div>
                    <Link
                        to={APP_ROUTES.public.courses}
                        className="hidden text-sm text-primary hover:underline md:inline"
                    >
                        {t('categories.viewAll')}
                    </Link>
                </div>

                {renderContent()}

                <div className="mt-8 flex justify-center md:hidden">
                    <Link
                        to={APP_ROUTES.public.courses}
                        className="rounded-full bg-primary/10 px-6 py-2.5 text-sm font-medium text-primary transition-colors hover:bg-primary/20"
                    >
                        {t('categories.viewAll')}
                    </Link>
                </div>
            </div>
        </section>
    );
}
