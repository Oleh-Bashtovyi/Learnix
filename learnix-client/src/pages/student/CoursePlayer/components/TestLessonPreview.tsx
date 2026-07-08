import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import { CheckCircle2, ClipboardList, Clock, XCircle } from 'lucide-react';
import { useMarkLessonComplete } from '@/hooks/lesson/useMarkLessonComplete';
import { useMyTestAttempts } from '@/hooks/lesson/useMyTestAttempts';
import { useTestLesson } from '@/hooks/lesson/useTestLesson';
import { TestAttemptHistory } from '@/pages/student/TestLesson/components/TestAttemptHistory';
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
                    <div>
                        <p className="font-semibold">{t('testPreview.heading')}</p>
                        {!isLoading && test && (
                            <p className="text-sm text-muted-foreground">
                                {t('testPreview.questionsCount', { count: test.questions.length })}
                            </p>
                        )}
                    </div>
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
                    <div className="space-y-4">
                        <div className="flex flex-wrap gap-4 text-sm text-muted-foreground">
                            <span>
                                {t('testPreview.passThreshold', { pct: test.passingThreshold })}
                            </span>
                            <span>
                                {test.attemptLimit
                                    ? t('testPreview.attemptsLimit', { n: test.attemptLimit })
                                    : t('testPreview.unlimitedAttempts')}
                            </span>
                            {status && (
                                <span>
                                    {t('testPreview.attemptsUsed', { n: status.attemptsUsed })}
                                </span>
                            )}
                        </div>

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

            {/* Attempt History */}
            {!isLoading && !isEmpty && test && attempts.length > 0 && (
                <div className="mt-8">
                    <TestAttemptHistory attempts={attempts} />
                </div>
            )}
        </div>
    );
}
