import { useQuery } from '@tanstack/react-query';
import { queryKeys } from '@/api/queryKeys';
import { categoriesApi } from '@/api/categories.api';
import { getCategoryVisuals, type LandingCategory } from '@/mocks/landing.mock';

export function useCategories() {
    return useQuery<LandingCategory[]>({
        queryKey: queryKeys.categories.lists(),
        queryFn: async () => {
            const apiCategories = await categoriesApi.getAll();
            return apiCategories.map((apiCat): LandingCategory => {
                const visual = getCategoryVisuals(apiCat.slug);
                return {
                    id: apiCat.id,
                    name: apiCat.name,
                    slug: apiCat.slug,
                    imageUrl: apiCat.imageUrl,
                    coursesCount: apiCat.coursesCount,
                    emoji: visual.emoji,
                    iconBgClass: visual.iconBgClass,
                    iconTextClass: visual.iconTextClass,
                };
            });
        },
        staleTime: 1000 * 60 * 5,
    });
}
