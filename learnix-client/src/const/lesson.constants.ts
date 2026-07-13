import type { LucideIcon } from 'lucide-react';
import { Eye, EyeOff, Lock, ScanEye } from 'lucide-react';
import type { StatTone } from '@/components/common/ui/StatTile';
import { TestReviewMode } from '@/enums/lesson.enums';

export const LESSON_LIMITS = {
    TITLE_MAX: 300,
    DESCRIPTION_MAX: 2000,
    POST_CONTENT_MAX: 50000,
    PASSING_THRESHOLD_MIN: 1,
    PASSING_THRESHOLD_MAX: 100,
    ATTEMPT_LIMIT_MIN: 1,
    COOLDOWN_MINUTES_MIN: 1,
    QUESTION_OPTIONS_MIN: 2,
    QUESTION_OPTIONS_MAX: 6,
    /** Hard cap on a student's free-text answer to a TextInput question. */
    TEXT_ANSWER_MAX: 500,
} as const;

/**
 * Review modes in the order the instructor should meet them: most disclosed first, so the default
 * (and the platform's previous behaviour) sits at the top of the list rather than buried under the
 * restrictive ones.
 */
export const REVIEW_MODE_ORDER = [
    TestReviewMode.FullReview,
    TestReviewMode.AnswersAndCorrectness,
    TestReviewMode.AnswersOnly,
    TestReviewMode.ScoreOnly,
] as const;

/**
 * How each review mode looks to a student. The four modes are a ladder of openness, and the palette
 * says so before a word is read: green wide open, then blue, then amber, then red for a test that
 * gives nothing back. Rendered as four identical grey paragraphs, they could only be told apart by
 * reading them — which defeats the point of announcing the policy before the test starts.
 *
 * Text lives in i18n (`testPreview.reviewMode.*`); this holds only what is not text.
 */
export const REVIEW_MODE_VISUALS: Record<TestReviewMode, { icon: LucideIcon; tone: StatTone }> = {
    [TestReviewMode.FullReview]: { icon: Eye, tone: 'success' },
    [TestReviewMode.AnswersAndCorrectness]: { icon: ScanEye, tone: 'brand' },
    [TestReviewMode.AnswersOnly]: { icon: EyeOff, tone: 'warning' },
    [TestReviewMode.ScoreOnly]: { icon: Lock, tone: 'destructive' },
};
