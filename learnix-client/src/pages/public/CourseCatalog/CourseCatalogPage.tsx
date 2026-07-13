import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { SlidersHorizontal, X } from 'lucide-react';
import { CourseCard } from '@/components/common/course/CourseCard';
import { Seo } from '@/components/common/seo/Seo';
import { QueryError } from '@/components/common/system/QueryError';
import { PageSizeSelect } from '@/components/common/ui/PageSizeSelect';
import { Pagination } from '@/components/common/ui/Pagination';
import { SearchInput } from '@/components/common/ui/SearchInput';
import { CATALOG_PAGE_SIZES } from '@/const/ui.constants';
import { useCatalogCourses } from '@/hooks/course/useCatalogCourses';
import { useCategories } from '@/hooks/course/useCategories';
import { useMediaQuery } from '@/hooks/shared/useMediaQuery';
import { APP_ROUTES } from '@/routes/paths';
import { cn } from '@/utils/cn';
import { breadcrumbJsonLd } from '@/utils/seo';
import { FilterSidebar } from './FilterSidebar';
import { SortDropdown } from './SortDropdown';
import { useCatalogFilters } from './hooks/useCatalogFilters';

export default function CourseCatalogPage() {
    const { t } = useTranslation('catalog');

    const {
        categoryId,
        sortBy,
        isFree,
        minRating,
        page,
        pageSize,
        searchInput,
        debouncedSearch,
        setSearchInput,
        setPage,
        setPageSize,
        setCategoryId,
        setSortBy,
        setIsFree,
        setMinRating,
        clearAllFilters,
    } = useCatalogFilters();

    const [isFiltersOpen, setIsFiltersOpen] = useState(false);

    // Mobile never offers 48+ cards per page. If the URL carries a desktop-only size and the
    // viewport is small, clamp it down to the largest mobile option.
    const isDesktop = useMediaQuery('(min-width: 640px)');
    const pageSizeOptions = isDesktop ? CATALOG_PAGE_SIZES.desktop : CATALOG_PAGE_SIZES.mobile;
    useEffect(() => {
        const max = pageSizeOptions.at(-1);
        if (max !== undefined && pageSize > max) setPageSize(max);
    }, [pageSizeOptions, pageSize, setPageSize]);

    const { data: categoriesData } = useCategories();
    const categories = categoriesData ?? [];

    const { data, isFetching, isLoading, isError, refetch } = useCatalogCourses({
        search: debouncedSearch,
        categoryId,
        sortBy,
        isFree,
        minRating,
        page,
        pageSize,
    });

    const courses = data?.items ?? [];
    const totalCount = data?.totalCount ?? 0;
    const totalPages = data?.totalPages ?? 1;

    // Active chips
    const chips: { label: string; onRemove: () => void }[] = [];
    if (categoryId) {
        const cat = categories.find((c) => c.id === categoryId);
        if (cat) chips.push({ label: cat.name, onRemove: () => setCategoryId('') });
    }
    if (isFree === true)
        chips.push({
            label: t('common:general.free'),
            onRemove: () => setIsFree(undefined),
        });
    if (isFree === false)
        chips.push({
            label: t('activeChips.paid'),
            onRemove: () => setIsFree(undefined),
        });
    if (minRating !== undefined)
        chips.push({
            label: t('activeChips.rating', { n: minRating }),
            onRemove: () => setMinRating(undefined),
        });

    return (
        <>
            {/* Filters and paging live in the query string; canonical points at the clean URL so
                every filtered view consolidates onto one indexable page. */}
            <Seo
                title={t('seo.title')}
                description={t('seo.description')}
                canonicalPath={APP_ROUTES.public.courses}
                jsonLd={breadcrumbJsonLd([
                    { name: t('common:navigation.home'), path: APP_ROUTES.public.home },
                    { name: t('common:navigation.courses'), path: APP_ROUTES.public.courses },
                ])}
            />
            <div className="min-h-screen bg-background">
                <div className="mx-auto max-w-7xl px-4 pb-8 pt-4 sm:px-6 md:pt-6">
                    {/* Page title. Left-aligned on a phone like everything under it — centring it there
                        made the one heading on the page the only thing that did not line up with the
                        column, and forced the count onto a line of its own. */}
                    <div className="mb-4 flex flex-col justify-between gap-1 md:mb-6 md:flex-row md:items-end md:justify-start md:gap-4">
                        <h1 className="font-heading text-2xl font-bold md:text-4xl">
                            {t('common:navigation.allCourses')}
                        </h1>
                        <p className="text-sm text-muted-foreground md:pb-1 md:text-base">
                            {debouncedSearch
                                ? t('resultsCountQuery', {
                                      count: totalCount,
                                      query: debouncedSearch,
                                  })
                                : t('resultsCount', { count: totalCount })}
                        </p>
                    </div>

                    {/* Body: sidebar + courses.
                        Explicit grid placement, so the DOM order can be the one a phone needs while the
                        desktop layout stays a sidebar beside a column. On a phone the children fall in
                        source order — toolbar, then the filter panel it opens, then the cards — which is
                        exactly right; with the toolbar inside the cards column it could not have come
                        before the sidebar at all. */}
                    {/* `grid-rows-[auto_1fr]` is load-bearing. The sidebar spans both rows, and with
                        implicit auto rows a sidebar taller than the toolbar plus the cards spills its
                        surplus height into BOTH of them — the toolbar floats to the middle of a row far
                        taller than itself, leaving a chasm beneath it, and the cards drift to the middle
                        of theirs. Pinning row one to its content and letting row two absorb the rest puts
                        everything back at the top, where it belongs. */}
                    <div className="grid gap-4 md:grid-cols-[260px_1fr] md:grid-rows-[auto_1fr] md:gap-8">
                        {/* Toolbar: search leads, because searching is what people came to do. Filters
                            and sort share the row under it rather than taking one each — two full-width
                            bars for secondary controls cost a third of a phone screen. */}
                        <div className="md:col-start-2 md:row-start-1 md:flex md:items-center md:gap-3 md:self-start">
                            <SearchInput
                                containerClassName="md:flex-1"
                                value={searchInput}
                                onChange={(e) => setSearchInput(e.target.value)}
                                onClear={() => setSearchInput('')}
                                placeholder={t('searchPlaceholder')}
                            />

                            {/* Phone: filters and sort split one row. Desktop: the filter panel is always
                                open beside the results, so only sort belongs here. */}
                            <div className="mt-3 grid grid-cols-2 gap-3 md:hidden">
                                <button
                                    type="button"
                                    onClick={() => setIsFiltersOpen((prev) => !prev)}
                                    className={cn(
                                        'flex items-center justify-center gap-2 rounded-lg border px-4 py-2.5 text-sm font-medium shadow-sm transition-all active:scale-[0.98]',
                                        isFiltersOpen
                                            ? 'border-primary bg-primary/10 text-primary'
                                            : 'border-border bg-card hover:bg-secondary',
                                    )}
                                >
                                    <SlidersHorizontal className="size-4" />
                                    {t('common:general.filters')}
                                </button>
                                <SortDropdown value={sortBy} onChange={setSortBy} />
                            </div>

                            <div className="hidden md:block">
                                <SortDropdown value={sortBy} onChange={setSortBy} />
                            </div>
                        </div>

                        {/* Filters */}
                        <div
                            className={cn(
                                'md:col-start-1 md:row-span-2 md:row-start-1 md:block',
                                isFiltersOpen ? 'block' : 'hidden',
                            )}
                        >
                            <FilterSidebar
                                categories={categories}
                                selectedCategoryId={categoryId}
                                isFree={isFree}
                                minRating={minRating}
                                onCategoryChange={setCategoryId}
                                onPriceChange={setIsFree}
                                onRatingChange={setMinRating}
                                onClear={clearAllFilters}
                            />
                        </div>

                        {/* Main */}
                        <div className="md:col-start-2 md:row-start-2 md:self-start">
                            {/* Active filter chips */}
                            {chips.length > 0 && (
                                <div className="mb-4 flex flex-wrap gap-2">
                                    {chips.map((chip) => (
                                        <span
                                            key={chip.label}
                                            className="flex items-center gap-1 rounded-full bg-primary/10 px-3 py-1 text-xs text-primary"
                                        >
                                            {chip.label}
                                            <button
                                                type="button"
                                                onClick={chip.onRemove}
                                                className="ml-0.5 rounded-full hover:text-primary/70"
                                                aria-label={`Remove ${chip.label} filter`}
                                            >
                                                <X className="size-3" />
                                            </button>
                                        </span>
                                    ))}
                                </div>
                            )}

                            {/* Grid */}
                            {isLoading && courses.length === 0 ? (
                                <div className="grid gap-5 sm:grid-cols-2 lg:grid-cols-3">
                                    {Array.from({ length: 6 }).map((_, i) => (
                                        <div
                                            key={i}
                                            className="h-72 animate-pulse rounded-xl border border-border bg-card"
                                        />
                                    ))}
                                </div>
                            ) : isError ? (
                                <QueryError
                                    message={t('error.title')}
                                    onRetry={refetch}
                                    retryLabel={t('common:actions.tryAgain')}
                                />
                            ) : (
                                <div
                                    className={cn(
                                        'grid gap-5 transition-opacity sm:grid-cols-2 lg:grid-cols-3',
                                        isFetching && 'opacity-60',
                                    )}
                                >
                                    {courses.length > 0
                                        ? courses.map((course) => (
                                              <CourseCard key={course.id} course={course} />
                                          ))
                                        : !isFetching && (
                                              <div className="col-span-full py-20 text-center">
                                                  <p className="font-heading text-lg font-semibold text-foreground">
                                                      {t('noResultsTitle')}
                                                  </p>
                                                  <p className="mt-2 text-sm text-muted-foreground">
                                                      {t('noResultsDesc')}
                                                  </p>
                                                  <button
                                                      type="button"
                                                      onClick={clearAllFilters}
                                                      className="mt-4 text-sm text-primary underline hover:text-primary/80"
                                                  >
                                                      {t('clearFilters')}
                                                  </button>
                                              </div>
                                          )}
                                </div>
                            )}

                            {/* Footer: page size (left) + pagination (right) */}
                            {courses.length > 0 && (
                                <div className="flex flex-col items-center gap-4 pt-10 sm:flex-row sm:justify-between">
                                    <PageSizeSelect
                                        value={pageSize}
                                        onChange={setPageSize}
                                        options={pageSizeOptions}
                                        label={t('pagination.perPage')}
                                    />
                                    <Pagination
                                        page={page}
                                        totalPages={totalPages}
                                        onChange={setPage}
                                        showGoToPage={totalPages > 10}
                                        goToLabel={t('pagination.goToPage')}
                                    />
                                </div>
                            )}
                        </div>
                    </div>
                </div>
            </div>
        </>
    );
}
