import { useQuery } from '@tanstack/react-query';
import { queryKeys } from '@/api/queryKeys';
import { categoriesApi } from '@/api/categories.api';
import { landingCategories, type LandingCategory } from '@/mocks/landing.mock';

/**
 * Fetches categories from the backend and merges local presentation data
 * (emoji, iconBgClass, iconTextClass, coursesCount).
 *
 * The backend GET /api/categories returns { id, name, slug, imageUrl } —
 * it does not include coursesCount. The visual metadata (emoji/colors)
 * is a frontend concern mapped by slug.
 *
 * Falls back to full mock data if the API is unreachable.
 */
export function useCategories() {
    return useQuery<LandingCategory[]>({
        queryKey: queryKeys.categories.lists(),
        queryFn: async () => {
            try {
                const apiCategories = await categoriesApi.getAll();

                const mockBySlug = new Map(landingCategories.map((c) => [c.slug, c]));

                const merged = apiCategories.map((apiCat): LandingCategory => {
                    const mock = mockBySlug.get(apiCat.slug);
                    return {
                        id: apiCat.id,
                        name: apiCat.name,
                        slug: apiCat.slug,
                        coursesCount: mock?.coursesCount ?? 0,
                        emoji: mock?.emoji ?? '📚',
                        iconBgClass: mock?.iconBgClass ?? 'bg-primary/10',
                        iconTextClass: mock?.iconTextClass ?? 'text-primary',
                    };
                });

                return merged.length > 0 ? merged : landingCategories;
            } catch {
                return landingCategories;
            }
        },
        staleTime: 1000 * 60 * 5,
    });
}
