import { useEffect, useRef, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { Search, X } from 'lucide-react';
import { CourseCard } from '@/components/common/CourseCard';
import { useCategories } from '@/hooks/useCategories';
import { useCatalogCourses } from '@/hooks/useCatalogCourses';
import { COURSE_CATALOG } from '@/const/localization/courseCatalog';
import { cn } from '@/utils/cn';
import { FilterSidebar } from './FilterSidebar';
import { SortDropdown } from './SortDropdown';

const PAGE_SIZE = 12;

type SortBy = 'popular' | 'newest' | 'rating';

function useDebounce<T>(value: T, delay: number): T {
    const [debounced, setDebounced] = useState(value);
    useEffect(() => {
        const timer = setTimeout(() => setDebounced(value), delay);
        return () => clearTimeout(timer);
    }, [value, delay]);
    return debounced;
}

function Pagination({
    page,
    totalPages,
    onChange,
}: {
    page: number;
    totalPages: number;
    onChange: (p: number) => void;
}) {
    if (totalPages <= 1) return null;

    const pages: (number | '...')[] = [];
    if (totalPages <= 7) {
        for (let i = 1; i <= totalPages; i++) pages.push(i);
    } else {
        pages.push(1);
        if (page > 3) pages.push('...');
        for (let i = Math.max(2, page - 1); i <= Math.min(totalPages - 1, page + 1); i++)
            pages.push(i);
        if (page < totalPages - 2) pages.push('...');
        pages.push(totalPages);
    }

    return (
        <div className="flex justify-center gap-1.5 pt-10">
            <button
                onClick={() => onChange(page - 1)}
                disabled={page === 1}
                className="grid h-9 w-9 place-items-center rounded-lg border border-border hover:bg-secondary disabled:cursor-not-allowed disabled:opacity-40"
            >
                {COURSE_CATALOG.PAGINATION.PREV}
            </button>
            {pages.map((p, idx) =>
                p === '...' ? (
                    <span
                        key={`ellipsis-${idx}`}
                        className="grid h-9 w-9 place-items-center text-muted-foreground"
                    >
                        …
                    </span>
                ) : (
                    <button
                        key={p}
                        onClick={() => onChange(p)}
                        className={cn(
                            'grid h-9 w-9 place-items-center rounded-lg text-sm font-medium transition-colors',
                            p === page
                                ? 'bg-primary text-primary-foreground'
                                : 'border border-border hover:bg-secondary',
                        )}
                    >
                        {p}
                    </button>
                ),
            )}
            <button
                onClick={() => onChange(page + 1)}
                disabled={page === totalPages}
                className="grid h-9 w-9 place-items-center rounded-lg border border-border hover:bg-secondary disabled:cursor-not-allowed disabled:opacity-40"
            >
                {COURSE_CATALOG.PAGINATION.NEXT}
            </button>
        </div>
    );
}

export default function CourseCatalogPage() {
    const [searchParams, setSearchParams] = useSearchParams();

    // Read URL state
    const searchParam = searchParams.get('search') ?? '';
    const categoryId = searchParams.get('categoryId') ?? '';
    const sortBy = (searchParams.get('sortBy') as SortBy) ?? 'popular';
    const isFreeParam = searchParams.get('isFree');
    const isFree: boolean | undefined =
        isFreeParam === '1' ? true : isFreeParam === '0' ? false : undefined;
    const minRatingParam = searchParams.get('minRating');
    const minRating: number | undefined = minRatingParam ? parseFloat(minRatingParam) : undefined;
    const page = parseInt(searchParams.get('page') ?? '1', 10) || 1;

    // Local search input state (debounced before hitting URL/API)
    const [searchInput, setSearchInput] = useState(searchParam);
    const debouncedSearch = useDebounce(searchInput, 350);
    const isFirstMount = useRef(true);

    useEffect(() => {
        if (isFirstMount.current) {
            isFirstMount.current = false;
            return;
        }
        setParam('search', debouncedSearch || null);
        setParam('page', null); // reset pagination on search change
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [debouncedSearch]);

    function setParam(key: string, value: string | null) {
        setSearchParams((prev) => {
            const next = new URLSearchParams(prev);
            if (value === null || value === '') next.delete(key);
            else next.set(key, value);
            return next;
        });
    }

    function setPage(p: number) {
        setParam('page', p === 1 ? null : String(p));
    }

    function setCategoryId(id: string) {
        setSearchParams((prev) => {
            const next = new URLSearchParams(prev);
            if (!id) next.delete('categoryId');
            else next.set('categoryId', id);
            next.delete('page');
            return next;
        });
    }

    function setSortBy(val: SortBy) {
        setSearchParams((prev) => {
            const next = new URLSearchParams(prev);
            if (val === 'popular') next.delete('sortBy');
            else next.set('sortBy', val);
            next.delete('page');
            return next;
        });
    }

    function setIsFree(val: boolean | undefined) {
        setSearchParams((prev) => {
            const next = new URLSearchParams(prev);
            if (val === undefined) next.delete('isFree');
            else next.set('isFree', val ? '1' : '0');
            next.delete('page');
            return next;
        });
    }

    function setMinRating(val: number | undefined) {
        setSearchParams((prev) => {
            const next = new URLSearchParams(prev);
            if (val === undefined) next.delete('minRating');
            else next.set('minRating', String(val));
            next.delete('page');
            return next;
        });
    }

    function clearAllFilters() {
        setSearchInput('');
        setSearchParams(new URLSearchParams());
    }

    const { data: categoriesData } = useCategories();
    const categories = categoriesData ?? [];

    const { data, isFetching } = useCatalogCourses({
        search: debouncedSearch,
        categoryId,
        sortBy,
        isFree,
        minRating,
        page,
        pageSize: PAGE_SIZE,
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
            label: COURSE_CATALOG.ACTIVE_CHIPS.FREE,
            onRemove: () => setIsFree(undefined),
        });
    if (isFree === false)
        chips.push({
            label: COURSE_CATALOG.ACTIVE_CHIPS.PAID,
            onRemove: () => setIsFree(undefined),
        });
    if (minRating !== undefined)
        chips.push({
            label: COURSE_CATALOG.ACTIVE_CHIPS.RATING(minRating),
            onRemove: () => setMinRating(undefined),
        });

    return (
        <div className="min-h-screen bg-background">
            <div className="mx-auto max-w-7xl px-4 py-8 sm:px-6">
                {/* Page title */}
                <div className="mb-5">
                    <h1 className="font-heading text-3xl font-bold">{COURSE_CATALOG.PAGE_TITLE}</h1>
                    <p className="mt-1 text-muted-foreground">
                        {COURSE_CATALOG.RESULTS_COUNT(totalCount, debouncedSearch || undefined)}
                    </p>
                </div>

                {/* Body: sidebar + courses */}
                <div className="grid gap-8 md:grid-cols-[260px_1fr]">
                    {/* Filters */}
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

                    {/* Main */}
                    <div>
                        {/* Search + Sort row */}
                        <div className="mb-4 flex items-center gap-3">
                            <div className="relative flex-1">
                                <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
                                <input
                                    type="text"
                                    value={searchInput}
                                    onChange={(e) => setSearchInput(e.target.value)}
                                    placeholder={COURSE_CATALOG.SEARCH_PLACEHOLDER}
                                    className="w-full rounded-lg border border-input bg-card py-2.5 pl-9 pr-9 text-sm focus:outline-none focus:ring-2 focus:ring-ring"
                                />
                                {searchInput && (
                                    <button
                                        type="button"
                                        onClick={() => setSearchInput('')}
                                        className="absolute right-3 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground"
                                    >
                                        <X className="h-4 w-4" />
                                    </button>
                                )}
                            </div>
                            <SortDropdown value={sortBy} onChange={setSortBy} />
                        </div>

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
                                            <X className="h-3 w-3" />
                                        </button>
                                    </span>
                                ))}
                            </div>
                        )}

                        {/* Grid */}
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
                                              {COURSE_CATALOG.NO_RESULTS_TITLE}
                                          </p>
                                          <p className="mt-2 text-sm text-muted-foreground">
                                              {COURSE_CATALOG.NO_RESULTS_DESC}
                                          </p>
                                          <button
                                              type="button"
                                              onClick={clearAllFilters}
                                              className="mt-4 text-sm text-primary underline hover:text-primary/80"
                                          >
                                              {COURSE_CATALOG.CLEAR_FILTERS}
                                          </button>
                                      </div>
                                  )}
                        </div>

                        <Pagination page={page} totalPages={totalPages} onChange={setPage} />
                    </div>
                </div>
            </div>
        </div>
    );
}
