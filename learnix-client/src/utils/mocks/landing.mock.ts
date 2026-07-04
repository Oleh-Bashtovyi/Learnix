import type { CategoryDto } from '@/types/course.types';

export interface LandingCategory extends CategoryDto {
    imageUrl: string | null;
    emoji: string;
    iconBgClass: string;
    iconTextClass: string;
}

interface CategoryVisuals {
    emoji: string;
    iconBgClass: string;
    iconTextClass: string;
}

export const CATEGORY_VISUALS: Record<string, CategoryVisuals> = {
    // Backend seeded categories
    programming: { emoji: '💻', iconBgClass: 'bg-primary/10', iconTextClass: 'text-primary' },
    'web-development': { emoji: '🌐', iconBgClass: 'bg-accent/10', iconTextClass: 'text-accent' },
    'data-science': { emoji: '📊', iconBgClass: 'bg-primary/10', iconTextClass: 'text-primary' },
    design: { emoji: '🎨', iconBgClass: 'bg-warning/20', iconTextClass: 'text-warning' },
    business: { emoji: '💼', iconBgClass: 'bg-success/20', iconTextClass: 'text-success' },
    marketing: { emoji: '📈', iconBgClass: 'bg-warning/20', iconTextClass: 'text-warning' },
    'personal-development': {
        emoji: '🌟',
        iconBgClass: 'bg-accent/10',
        iconTextClass: 'text-accent',
    },
    'language-learning': {
        emoji: '🗣️',
        iconBgClass: 'bg-success/20',
        iconTextClass: 'text-success',
    },

    // Other predefined categories
    'web-dev': { emoji: '💻', iconBgClass: 'bg-primary/10', iconTextClass: 'text-primary' },
    backend: { emoji: '⚙️', iconBgClass: 'bg-accent/10', iconTextClass: 'text-accent' },
    mobile: { emoji: '📱', iconBgClass: 'bg-success/20', iconTextClass: 'text-success' },
    devops: { emoji: '☁️', iconBgClass: 'bg-destructive/10', iconTextClass: 'text-destructive' },
    data: { emoji: '📊', iconBgClass: 'bg-primary/10', iconTextClass: 'text-primary' },
    'ai-ml': { emoji: '🤖', iconBgClass: 'bg-accent/10', iconTextClass: 'text-accent' },
    security: { emoji: '🔒', iconBgClass: 'bg-destructive/10', iconTextClass: 'text-destructive' },
    'game-dev': { emoji: '🎮', iconBgClass: 'bg-primary/10', iconTextClass: 'text-primary' },
    architecture: { emoji: '📐', iconBgClass: 'bg-accent/10', iconTextClass: 'text-accent' },
};

export function getCategoryVisuals(slug: string): CategoryVisuals {
    const visual = CATEGORY_VISUALS[slug];
    return {
        emoji: visual?.emoji ?? '📚',
        iconBgClass: visual?.iconBgClass ?? 'bg-primary/10',
        iconTextClass: visual?.iconTextClass ?? 'text-primary',
    };
}
