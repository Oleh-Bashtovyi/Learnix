import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { BookOpen } from 'lucide-react';
import { cn } from '@/utils/cn';
import { QueryError } from '@/components/common/QueryError';
import type { LandingCategory } from '@/mocks/landing.mock';

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
                <div className="grid grid-cols-2 gap-4 md:grid-cols-3 lg:grid-cols-6">
                    {Array.from({ length: 12 }).map((_, i) => (
                        <div
                            key={i}
                            className="h-28 animate-pulse rounded-xl border border-border bg-card"
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
                    <BookOpen className="h-10 w-10 text-muted-foreground/40" />
                    <p className="text-sm text-muted-foreground">{t('categories.empty')}</p>
                </div>
            );
        }

        return (
            <div className="grid grid-cols-2 gap-4 md:grid-cols-3 lg:grid-cols-6">
                {categories.map((cat) => (
                    <Link
                        key={cat.id}
                        to={`/courses?categoryId=${cat.id}`}
                        className="group rounded-xl border border-border bg-card p-5 transition-all hover:border-primary hover:shadow-md"
                    >
                        <div
                            className={cn(
                                'grid h-12 w-12 place-items-center rounded-lg text-2xl transition-transform group-hover:scale-110',
                                cat.iconBgClass,
                                cat.iconTextClass,
                            )}
                        >
                            {cat.emoji}
                        </div>
                        <h3 className="mt-4 font-heading font-semibold">{cat.name}</h3>
                        <p className="mt-1 text-xs text-muted-foreground">
                            {cat.coursesCount} {t('categories.coursesLabel')}
                        </p>
                    </Link>
                ))}
            </div>
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
                        to="/courses"
                        className="hidden text-sm text-primary hover:underline md:inline"
                    >
                        {t('categories.viewAll')}
                    </Link>
                </div>

                {renderContent()}
            </div>
        </section>
    );
}
