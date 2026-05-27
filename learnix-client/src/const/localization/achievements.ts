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
    name: string;
    description: string;
    icon: LucideIcon;
    /** Placeholder gradient [from, to] — used until a real cover image is provided */
    gradient: [string, string];
}

export const ACHIEVEMENT_META: Record<string, AchievementMeta> = {
    FIRST_LESSON: {
        name: 'First Step',
        description: 'Complete your very first lesson',
        icon: BookOpen,
        gradient: ['#93c5fd', '#3b82f6'],
    },
    LESSONS_50: {
        name: 'Eager Learner',
        description: 'Complete 50 lessons',
        icon: Flame,
        gradient: ['#fdba74', '#f97316'],
    },
    LESSONS_200: {
        name: 'Knowledge Seeker',
        description: 'Complete 200 lessons',
        icon: Star,
        gradient: ['#c084fc', '#9333ea'],
    },
    LESSONS_500: {
        name: 'Scholar',
        description: 'Complete 500 lessons',
        icon: Layers,
        gradient: ['#fcd34d', '#d97706'],
    },
    FIRST_COURSE: {
        name: 'Course Graduate',
        description: 'Complete your first course',
        icon: GraduationCap,
        gradient: ['#6ee7b7', '#059669'],
    },
    COURSES_3: {
        name: 'Triple Crown',
        description: 'Complete 3 courses',
        icon: Trophy,
        gradient: ['#fca5a5', '#dc2626'],
    },
    COURSES_5: {
        name: 'Dedicated Student',
        description: 'Complete 5 courses',
        icon: Award,
        gradient: ['#a78bfa', '#7c3aed'],
    },
    SPEED_DEMON: {
        name: 'Speed Demon',
        description: 'Complete a 20+ question test in under 5 minutes',
        icon: Zap,
        gradient: ['#fde047', '#ca8a04'],
    },
    POLYMATH: {
        name: 'Polymath',
        description: 'Complete courses in 3 or more different categories',
        icon: Globe,
        gradient: ['#67e8f9', '#0891b2'],
    },
    PROFILE_COMPLETE: {
        name: 'All Set',
        description: 'Fill in your bio and set a profile photo',
        icon: CheckCircle2,
        gradient: ['#86efac', '#16a34a'],
    },
} as const;

/** Ordered list of all achievement codes */
export const ALL_ACHIEVEMENT_CODES = Object.keys(ACHIEVEMENT_META);

export const ACHIEVEMENTS_PAGE = {
    TITLE: 'Achievements',
    SUBTITLE: 'Track your learning milestones',
    PROGRESS_SECTION: 'Your Progress',
    STATS: {
        LESSONS: 'Lessons Completed',
        COURSES: 'Courses Completed',
        CATEGORIES: 'Categories Explored',
    },
    EARNED_COUNT: (n: number) => `${n} achievement${n !== 1 ? 's' : ''} earned`,
    LOCKED_LABEL: 'Locked',
    EARNED_LABEL: 'Earned',
} as const;
