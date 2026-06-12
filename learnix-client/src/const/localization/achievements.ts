import type { LucideIcon } from 'lucide-react';
import {
    BookOpen,
    GraduationCap,
    Layers,
    Trophy,
    Zap,
    Globe,
    CheckCircle2,
    Star,
    Award,
    Flame,
} from 'lucide-react';

export interface AchievementMeta {
    icon: LucideIcon;
    gradient: [string, string];
}

export const ACHIEVEMENT_META: Record<string, AchievementMeta> = {
    FIRST_LESSON: { icon: BookOpen, gradient: ['#93c5fd', '#3b82f6'] },
    LESSONS_50: { icon: Flame, gradient: ['#fdba74', '#f97316'] },
    LESSONS_200: { icon: Star, gradient: ['#c084fc', '#9333ea'] },
    LESSONS_500: { icon: Layers, gradient: ['#fcd34d', '#d97706'] },
    FIRST_COURSE: { icon: GraduationCap, gradient: ['#6ee7b7', '#059669'] },
    COURSES_3: { icon: Trophy, gradient: ['#fca5a5', '#dc2626'] },
    COURSES_5: { icon: Award, gradient: ['#a78bfa', '#7c3aed'] },
    SPEED_DEMON: { icon: Zap, gradient: ['#fde047', '#ca8a04'] },
    POLYMATH: { icon: Globe, gradient: ['#67e8f9', '#0891b2'] },
    PROFILE_COMPLETE: { icon: CheckCircle2, gradient: ['#86efac', '#16a34a'] },
} as const;

export const ALL_ACHIEVEMENT_CODES = Object.keys(ACHIEVEMENT_META);
