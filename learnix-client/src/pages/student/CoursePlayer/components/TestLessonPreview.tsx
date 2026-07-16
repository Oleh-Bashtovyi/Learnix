import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import {
    CheckCircle2,
    ClipboardList,
    Clock,
    ListChecks,
    RotateCcw,
    Target,
    XCircle,
} from 'lucide-react';
import { MarkdownRenderer } from '@/components/common/ui/MarkdownRenderer';
import { StatTile, type StatTone } from '@/components/common/ui/StatTile';
import { REVIEW_MODE_VISUALS } from '@/const/lesson.constants';
import { TestReviewMode } from '@/enums/lesson.enums';
import { useMarkLessonComplete } from '@/hooks/lesson/useMarkLessonComplete';
import { useMyTestAttempts } from '@/hooks/lesson/useMyTestAttempts';
import { formatCooldown, useTestCooldown } from '@/hooks/lesson/useTestCooldown';
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

    // Ticks down and invalidates the test query on expiry, so the action unblocks without a reload.
    const cooldownSeconds = useTestCooldown(
        courseId,
        lesson.lessonId,
        status?.cooldownRemainingMinutes,
    );

    /** The single action for this test: a live link, or the same slot spelling out what blocks it.
     *
     *  Blocked does not mean illegible — the countdown is the one thing a student on cooldown needs
     *  to read. It gets full-contrast text and a shadow to lift it off the tinted result banner it
     *  sits on, while cursor, the absent hover and the secondary fill still say it is dead.
     *
     *  The fill is `secondary` and not `background`: --background is the page behind the card, which
     *  is *lighter* than --card in the light theme and darker in the dark one, so a button painted
     *  with it turns white-on-white in one theme and a black hole in the other. --secondary is a
     *  surface token — it contrasts against --card in both. */
    function renderAction() {
        const blockedClasses =
            'inline-flex cursor-not-allowed items-center gap-2 rounded-lg border border-border bg-secondary px-6 py-2.5 text-sm font-medium text-foreground shadow-sm';

        if (cooldownSeconds !== null) {
            return (
                <span className={blockedClasses}>
                    <Clock className="size-4 shrink-0" />
                    {t('testPreview.cooldownRemaining', formatCooldown(cooldownSeconds))}
                </span>
            );
        }

        if (status?.canAttempt === false) {
            return (
                <span className={blockedClasses}>
                    <XCircle className="size-4 shrink-0" />
                    {t('testPreview.noAttemptsLeft')}
                </span>
            );
        }

        return (
            <Link
                to={APP_ROUTES.student.testLesson(courseId, lesson.lessonId)}
                className="inline-flex items-center gap-2 rounded-lg bg-primary px-6 py-2.5 text-sm font-medium text-primary-foreground transition-colors hover:bg-primary/90"
            >
                <ClipboardList className="size-4" />
                {latest ? t('testPreview.retakeTest') : t('testPreview.startTest')}
            </Link>
        );
    }

    return (
        <div className="mx-auto max-w-3xl">
            <h1 className="mb-6 font-heading text-2xl font-bold">{lesson.title}</h1>

            <div className="rounded-xl border border-border bg-card p-8">
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
                            {/* The threshold is the one figure here a student has to walk away knowing —
                                it decides whether the attempt counted — so it is the only coloured tile
                                of the three. The question count and the attempts left are context. */}
                            <StatTile
                                icon={<ListChecks className="size-5" />}
                                tone="neutral"
                                label={t('testPreview.questionsLabel')}
                                value={String(test.questions.length)}
                            />
                            <StatTile
                                icon={<Target className="size-5" />}
                                tone="brand"
                                label={t('testPreview.passThresholdLabel')}
                                value={`${test.passingThreshold}%`}
                            />
                            {/* "1 of 2 used" wrapped onto a second line and made this tile taller than
                                the two beside it. The label already says these are attempts, so the
                                value only has to say how many of how many.

                                An unlimited test reads "2 used" rather than "2 / ∞": there is no
                                denominator to count against, and the fraction invited reading it as
                                two attempts out of infinity. */}
                            {(() => {
                                const attemptsUsed = status?.attemptsUsed ?? 0;
                                const attemptLimit = test.attemptLimit;

                                let attemptsTone: StatTone = 'success';
                                if (attemptLimit) {
                                    const attemptsLeft = attemptLimit - attemptsUsed;
                                    const ratio = attemptsUsed / attemptLimit;

                                    if (attemptsLeft <= 1) attemptsTone = 'destructive';
                                    else if (ratio < 0.5) attemptsTone = 'success';
                                    else if (ratio < 0.9) attemptsTone = 'warning';
                                    else attemptsTone = 'destructive';
                                }

                                return (
                                    <StatTile
                                        icon={<RotateCcw className="size-5" />}
                                        tone={attemptsTone}
                                        label={t('testPreview.attemptsLabel')}
                                        value={
                                            attemptLimit
                                                ? `${attemptsUsed} / ${attemptLimit}`
                                                : t('testPreview.attemptsUsed', {
                                                      n: attemptsUsed,
                                                  })
                                        }
                                    />
                                );
                            })()}
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
                            <div className="pt-4">
                                <MarkdownRenderer
                                    content={test.description}
                                    className="prose-sm text-muted-foreground"
                                />
                            </div>
                        )}

                        {/* The result and the action are one row. A cooldown and an exhausted attempt
                            limit are not news to announce — they are the reason the button cannot be
                            pressed, so they render as the disabled button itself rather than as a
                            second full-width banner stacked under the first. That leaves exactly one
                            coloured bar down here, and it is the one that carries a verdict. */}
                        {latest ? (
                            <div
                                className={cn(
                                    'flex flex-wrap items-center justify-between gap-4 rounded-lg border p-4',
                                    latest.passed
                                        ? 'border-success/30 bg-success/10'
                                        : 'border-destructive/30 bg-destructive/10',
                                )}
                            >
                                <div className="flex items-center gap-3">
                                    {latest.passed ? (
                                        <CheckCircle2 className="size-5 shrink-0 text-success" />
                                    ) : (
                                        <XCircle className="size-5 shrink-0 text-destructive" />
                                    )}
                                    <div>
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
                                {renderAction()}
                            </div>
                        ) : (
                            renderAction()
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
    neutral: 'border-border bg-muted-foreground/10 text-muted-foreground',
    success: 'border-success/30 bg-success/10 text-success',
    brand: 'border-brand/30 bg-brand/10 text-brand',
    accent: 'border-accent/30 bg-accent/10 text-accent-strong',
    warning: 'border-warning/30 bg-warning/10 text-warning',
    destructive: 'border-destructive/30 bg-destructive/10 text-destructive',
};
