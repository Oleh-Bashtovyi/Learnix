export const queryKeys = {
    courses: {
        all: ['courses'] as const,
        lists: () => [...queryKeys.courses.all, 'list'] as const,
        list: (filters: Record<string, unknown>) =>
            [...queryKeys.courses.lists(), filters] as const,
        featured: () => [...queryKeys.courses.all, 'featured'] as const,
        details: () => [...queryKeys.courses.all, 'detail'] as const,
        detail: (id: string) => [...queryKeys.courses.details(), id] as const,
    },
    categories: {
        all: ['categories'] as const,
        lists: () => [...queryKeys.categories.all, 'list'] as const,
    },
} as const;
