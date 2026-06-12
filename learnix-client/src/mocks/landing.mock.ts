import type { CategoryDto } from '@/types/course.types';

export interface LandingCategory extends CategoryDto {
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
    'web-dev': { emoji: '💻', iconBgClass: 'bg-primary/10', iconTextClass: 'text-primary' },
    backend: { emoji: '⚙️', iconBgClass: 'bg-accent/10', iconTextClass: 'text-accent' },
    design: { emoji: '🎨', iconBgClass: 'bg-warning/20', iconTextClass: 'text-warning' },
    mobile: { emoji: '📱', iconBgClass: 'bg-success/20', iconTextClass: 'text-success' },
    devops: { emoji: '☁️', iconBgClass: 'bg-destructive/10', iconTextClass: 'text-destructive' },
    data: { emoji: '📊', iconBgClass: 'bg-primary/10', iconTextClass: 'text-primary' },
    'ai-ml': { emoji: '🤖', iconBgClass: 'bg-accent/10', iconTextClass: 'text-accent' },
    marketing: { emoji: '📈', iconBgClass: 'bg-warning/20', iconTextClass: 'text-warning' },
    business: { emoji: '💼', iconBgClass: 'bg-success/20', iconTextClass: 'text-success' },
    security: { emoji: '🔒', iconBgClass: 'bg-destructive/10', iconTextClass: 'text-destructive' },
    'game-dev': { emoji: '🎮', iconBgClass: 'bg-primary/10', iconTextClass: 'text-primary' },
    architecture: { emoji: '📐', iconBgClass: 'bg-accent/10', iconTextClass: 'text-accent' },
};
