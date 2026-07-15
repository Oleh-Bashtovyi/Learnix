import type { LucideIcon } from 'lucide-react';
import {
    Award,
    BookOpen,
    CheckCircle2,
    Flame,
    Globe,
    GraduationCap,
    Layers,
    Star,
    Trophy,
    Zap,
} from 'lucide-react';

export interface AchievementMeta {
    icon: LucideIcon;
    gradient: [string, string];
}

export const ACHIEVEMENT_META: Record<string, AchievementMeta> = {
    FIRST_LESSON: { icon: BookOpen, gradient: ['#fef08a', '#eab308'] }, // Yellow/Gold
    LESSONS_50: { icon: Flame, gradient: ['#99f6e4', '#14b8a6'] }, // Teal
    LESSONS_200: { icon: Star, gradient: ['#e9d5ff', '#a855f7'] }, // Purple
    LESSONS_500: { icon: Layers, gradient: ['#f5d0fe', '#d946ef'] }, // Fuchsia/Purple
    FIRST_COURSE: { icon: GraduationCap, gradient: ['#86efac', '#22c55e'] }, // Green
    COURSES_3: { icon: Trophy, gradient: ['#fef08a', '#eab308'] }, // Yellow/Gold
    COURSES_5: { icon: Award, gradient: ['#bfdbfe', '#3b82f6'] }, // Blue
    SPEED_DEMON: { icon: Zap, gradient: ['#fecaca', '#ef4444'] }, // Red
    POLYMATH: { icon: Globe, gradient: ['#86efac', '#22c55e'] }, // Green
    PROFILE_COMPLETE: { icon: CheckCircle2, gradient: ['#bfdbfe', '#3b82f6'] }, // Blue
} as const;

export const ALL_ACHIEVEMENT_CODES = Object.keys(ACHIEVEMENT_META);

/**
 * How many badges the profile section shows on mobile before deferring to the achievements page.
 * The mobile grid is 3 columns wide, so this caps it at 3 rows; from `sm:` up the whole set fits.
 */
export const PROFILE_MOBILE_VISIBLE_ACHIEVEMENTS = 9;
