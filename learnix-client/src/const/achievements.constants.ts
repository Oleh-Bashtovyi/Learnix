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
import { ACHIEVEMENT_GRADIENTS } from '@/const/achievementColors.constants';

export interface AchievementMeta {
    icon: LucideIcon;
    gradient: [string, string];
}

export const ACHIEVEMENT_META: Record<string, AchievementMeta> = {
    FIRST_LESSON: { icon: BookOpen, gradient: ACHIEVEMENT_GRADIENTS.gold },
    LESSONS_50: { icon: Flame, gradient: ACHIEVEMENT_GRADIENTS.teal },
    LESSONS_200: { icon: Star, gradient: ACHIEVEMENT_GRADIENTS.purple },
    LESSONS_500: { icon: Layers, gradient: ACHIEVEMENT_GRADIENTS.fuchsia },
    FIRST_COURSE: { icon: GraduationCap, gradient: ACHIEVEMENT_GRADIENTS.green },
    COURSES_3: { icon: Trophy, gradient: ACHIEVEMENT_GRADIENTS.gold },
    COURSES_5: { icon: Award, gradient: ACHIEVEMENT_GRADIENTS.blue },
    SPEED_DEMON: { icon: Zap, gradient: ACHIEVEMENT_GRADIENTS.red },
    POLYMATH: { icon: Globe, gradient: ACHIEVEMENT_GRADIENTS.green },
    PROFILE_COMPLETE: { icon: CheckCircle2, gradient: ACHIEVEMENT_GRADIENTS.blue },
} as const;

export const ALL_ACHIEVEMENT_CODES = Object.keys(ACHIEVEMENT_META);

/**
 * How many badges the profile section shows on mobile before deferring to the achievements page.
 * The mobile grid is 3 columns wide, so this caps it at 3 rows; from `sm:` up the whole set fits.
 */
export const PROFILE_MOBILE_VISIBLE_ACHIEVEMENTS = 9;
