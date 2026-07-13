import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import {
    Infinity as InfinityIcon,
    CheckCircle2,
    ClipboardList,
    Clock,
    ListChecks,
    RotateCcw,
    Target,
    XCircle,
} from 'lucide-react';
import { StatTile, type StatTone } from '@/components/common/ui/StatTile';
import { REVIEW_MODE_VISUALS } from '@/const/lesson.constants';
import { TestReviewMode } from '@/enums/lesson.enums';
import { useMarkLessonComplete } from '@/hooks/lesson/useMarkLessonComplete';
import { useMyTestAttempts } from '@/hooks/lesson/useMyTestAttempts';
import { useTestLesson } from '@/hooks/lesson/useTestLesson';
import { TestAttemptHistory } from '@/pages/student/TestLesson/components/TestAttemptHistory';
import { TestAttemptReview } from '@/pages/student/TestLesson/components/TestAttemptReview';
import { APP_ROUTES } from '@/routes/paths';
import type { LessonProgressItemDto } from '@/types/progress.types';
import { cn } from '@/utils/cn';

interface TestLessonPreviewProps {
    lesson: LessonProgressItemDto;
    courseId: string;
}

export function TestLessonPreview({ lesson, courseId }: TestLessonPreviewProps) {
    const { t } = useTranslation('lessonPlayer');
    const { data: test, isLoading } = useTestLesson(courseId, lesson.lessonId);
    const { data: attempts = [] } = useMyTestAttempts(courseId, lesson.lessonId);
    const markComplete = useMarkLessonComplete(courseId);
    const [reviewAttemptId, setReviewAttemptId] = useState<string | null>(null);

    const reviewVisuals = REVIEW_MODE_VISUALS[test?.reviewMode ?? TestReviewMode.FullReview];

    const status = test?.studentStatus;
    const latest = status?.latestAttempt;
    const isEmpty = !isLoading && test !== undefined && test.questions.length === 0;

    return (
        <div className="mx-auto max-w-3xl">
            <h1 className="mb-6 font-heading text-2xl font-bold">{lesson.title}</h1>

            <div className="rounded-xl border border-border bg-card p-8">
                <div className="mb-6 flex items-center gap-3">
                    <div className="grid size-12 place-items-center rounded-lg bg-primary/10">
                        <ClipboardList className="size-6 text-primary" />
                    </div>
                    {/* The question count used to sit here as a subtitle; it is a stat, and it now
                        lives with the other stats rather than being said twice. */}
                    <p className="font-semibold">{t('testPreview.heading')}</p>
                </div>

                {isLoading && (
                    <div className="space-y-2">
                        {[1, 2, 3].map((i) => (
                            <div key={i} className="h-4 w-3/4 animate-pulse rounded bg-secondary" />
                        ))}
                    </div>
                )}

                {/* Empty test — no questions yet */}
                {isEmpty && (
                    <div className="space-y-4">
                        <p className="text-sm text-muted-foreground">
                            {t('testPreview.noQuestions')}
                        </p>
                        {lesson.isCompleted ? (
                            <span className="inline-flex items-center gap-2 rounded-lg bg-success/15 px-5 py-2 text-sm font-medium text-success">
                                <CheckCircle2 className="size-4" />
                                {t('common:status.completed')}
                            </span>
                        ) : (
                            <button
                                type="button"
                                onClick={() => markComplete.mutate(lesson.lessonId)}
                                disabled={markComplete.isPending}
                                className="inline-flex items-center gap-2 rounded-lg bg-primary px-5 py-2 text-sm font-medium text-primary-foreground transition-colors hover:bg-primary/90 disabled:opacity-50"
                            >
                                <CheckCircle2 className="size-4" />
                                {markComplete.isPending
                                    ? 'Saving...'
                                    : t('testPreview.markComplete')}
                            </button>
                        )}
                    </div>
                )}

                {!isLoading && !isEmpty && test && (
                    <div className="space-y-5">
                        <dl className="grid grid-cols-2 gap-3 sm:grid-cols-3">
                            <StatTile
                                icon={<ListChecks className="size-5" />}
                                tone="brand"
                                label={t('testPreview.questionsLabel')}
                                value={String(test.questions.length)}
                            />
                            <StatTile
                                icon={<Target className="size-5" />}
                                tone="accent"
                                label={t('testPreview.passThresholdLabel')}
                                value={`${test.passingThreshold}%`}
                            />
                            {/* "1 of 2 used" wrapped onto a second line and made this tile taller than
                                the two beside it. The label already says these are attempts, so the
                                value only has to say how many of how many.

                                No limit is drawn with the lucide glyph rather than the ∞ character:
                                the character is rendered by the heading font, which does not really
                                have one, and it showed. The icon is stroked like every other icon here. */}
                            <StatTile
                                icon={<RotateCcw className="size-5" />}
                                tone="success"
                                label={t('testPreview.attemptsLabel')}
                                value={
                                    <span className="flex items-center gap-1.5">
                                        {status?.attemptsUsed ?? 0} /{' '}
                                        {test.attemptLimit ?? (
                                            <InfinityIcon className="size-4 text-muted-foreground" />
                                        )}
                                    </span>
                                }
                            />
                        </dl>

                        {/* What the test gives back — worth knowing before starting, not after: a test
                            that never shows the answers is a different thing to sit than one that does.

                            The four modes are a ladder of openness, and the colour says which rung this
                            is before a word is read. As four identical grey paragraphs they could only
                            be told apart by reading them, which rather defeats announcing the policy. */}
                        <div
                            className={cn(
                                'flex items-start gap-3 rounded-xl border p-4',
                                REVIEW_TONE_CLASSES[reviewVisuals.tone],
                            )}
                        >
                            <div className="mt-0.5 shrink-0">
                                <reviewVisuals.icon className="size-5" />
                            </div>
                            <div className="min-w-0">
                                <p className="text-[11px] font-medium uppercase tracking-wide opacity-70">
                                    {t('testPreview.afterSubmitLabel')}
                                </p>
                                <p className="font-heading text-sm font-semibold">
                                    {t(`testPreview.reviewMode.${test.reviewMode}.title`)}
                                </p>
                                <p className="mt-0.5 text-sm text-muted-foreground">
                                    {t(`testPreview.reviewMode.${test.reviewMode}.detail`)}
                                </p>
                            </div>
                        </div>

                        {/* The instructor writes a description for the test and, until now, nobody
                            ever saw it — the field was fetched and dropped on the floor. It sits below
                            the facts rather than above them: it can run long, and the numbers a student
                            is deciding on should not be pushed off the first screen by prose. */}
                        {test.description && (
                            <p className="whitespace-pre-line text-sm leading-relaxed text-muted-foreground">
                                {test.description}
                            </p>
                        )}

                        {latest && (
                            <div
                                className={cn(
                                    'flex items-center gap-3 rounded-lg border p-4',
                                    latest.passed
                                        ? 'border-success/30 bg-success/10'
                                        : 'border-destructive/30 bg-destructive/10',
                                )}
                            >
                                {latest.passed ? (
                                    <CheckCircle2 className="size-5 shrink-0 text-success" />
                                ) : (
                                    <XCircle className="size-5 shrink-0 text-destructive" />
                                )}
                                <div className="flex-1">
                                    <p className="text-sm font-medium">
                                        {t('testPreview.lastResult')}
                                    </p>
                                    <p className="text-sm text-muted-foreground">
                                        {t('testPreview.score', {
                                            score: latest.score,
                                            max: latest.maxScore,
                                        })}{' '}
                                        &mdash;{' '}
                                        {latest.passed
                                            ? t('common:status.passed')
                                            : t('common:status.failed')}
                                    </p>
                                </div>
                            </div>
                        )}

                        {status?.cooldownRemainingMinutes ? (
                            <div className="flex items-center gap-2 rounded-lg border border-warning/30 bg-warning/10 p-4 text-sm">
                                <Clock className="size-4 text-warning" />
                                <span>
                                    {t('testPreview.cooldownRemaining', {
                                        min: status.cooldownRemainingMinutes,
                                    })}
                                </span>
                            </div>
                        ) : status?.canAttempt === false ? (
                            <div className="flex items-center gap-2 rounded-lg border border-destructive/30 bg-destructive/10 p-4 text-sm text-destructive">
                                <XCircle className="size-4 shrink-0" />
                                <span>{t('testPreview.noAttemptsLeft')}</span>
                            </div>
                        ) : (
                            <Link
                                to={APP_ROUTES.student.testLesson(courseId, lesson.lessonId)}
                                className="inline-flex items-center gap-2 rounded-lg bg-primary px-6 py-2.5 text-sm font-medium text-primary-foreground transition-colors hover:bg-primary/90"
                            >
                                <ClipboardList className="size-4" />
                                {latest ? t('testPreview.retakeTest') : t('testPreview.startTest')}
                            </Link>
                        )}
                    </div>
                )}
            </div>

            {/* Attempt history, and the review it opens onto. This is the page a student comes back to
                after the test, so it is where the past answers belong — a ScoreOnly test has none to
                show, and gets no review affordance rather than one that leads to an apology. */}
            {!isLoading && !isEmpty && test && attempts.length > 0 && (
                <div className="mt-8 space-y-6">
                    <TestAttemptHistory
                        attempts={attempts}
                        onReview={
                            test.reviewMode !== TestReviewMode.ScoreOnly
                                ? setReviewAttemptId
                                : undefined
                        }
                    />

                    {reviewAttemptId && (
                        <TestAttemptReview
                            courseId={courseId}
                            lessonId={lesson.lessonId}
                            attemptId={reviewAttemptId}
                            onClose={() => setReviewAttemptId(null)}
                        />
                    )}
                </div>
            )}
        </div>
    );
}

/** Border and fill for the review-mode panel, keyed by the tone its icon already carries. */
const REVIEW_TONE_CLASSES: Record<StatTone, string> = {
    success: 'border-success/30 bg-success/10 text-success',
    brand: 'border-brand/30 bg-brand/10 text-brand',
    accent: 'border-accent/30 bg-accent/10 text-accent',
    warning: 'border-warning/30 bg-warning/10 text-warning',
    destructive: 'border-destructive/30 bg-destructive/10 text-destructive',
};
