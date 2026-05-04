import { Link } from 'react-router-dom';
import type { LandingCategory } from '@/mocks/landing.mock';
import { cn } from '@/utils/cn';

interface CategoriesSectionProps {
    categories: LandingCategory[];
}

export function CategoriesSection({ categories }: CategoriesSectionProps) {
    return (
        <section id="categories" className="py-20">
            <div className="mx-auto max-w-7xl px-6">
                <div className="mb-10 flex items-end justify-between">
                    <div>
                        <span className="text-sm font-semibold text-primary">Browse</span>
                        <h2 className="mt-2 font-heading text-3xl font-bold md:text-4xl">
                            Explore by category
                        </h2>
                    </div>
                    <Link
                        to="/courses"
                        className="hidden text-sm text-primary hover:underline md:inline"
                    >
                        All categories →
                    </Link>
                </div>

                <div className="grid grid-cols-2 gap-4 md:grid-cols-3 lg:grid-cols-6">
                    {categories.map((cat) => (
                        <Link
                            key={cat.id}
                            to={`/courses?category=${cat.slug}`}
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
                            <p className="mt-1 text-xs text-muted-foreground">{cat.coursesCount} courses</p>
                        </Link>
                    ))}
                </div>
            </div>
        </section>
    );
}
