import { useTranslation } from 'react-i18next';
import { CheckCircle2, Eye, XCircle } from 'lucide-react';
import type { TestAttemptSummaryDto } from '@/types/lesson.types';
import { cn } from '@/utils/cn';

interface TestAttemptHistoryProps {
    attempts: TestAttemptSummaryDto[];
    /** Omitted when the test discloses nothing to review (ScoreOnly). */
    onReview?: (attemptId: string) => void;
}

export function TestAttemptHistory({ attempts, onReview }: TestAttemptHistoryProps) {
    const { t, i18n } = useTranslation('testLesson');

    if (attempts.length === 0) {
        return (
            <div className="rounded-xl border border-border bg-card p-4 sm:p-6">
                <h3 className="mb-4 font-heading text-lg font-semibold">{t('history.heading')}</h3>
                <p className="text-sm text-muted-foreground">{t('history.noAttempts')}</p>
            </div>
        );
    }

    const ordered = [...attempts].sort((a, b) => b.attemptNumber - a.attemptNumber);

    function formatDate(iso: string) {
        return new Date(iso).toLocaleDateString(i18n.language, {
            year: 'numeric',
            month: 'short',
            day: 'numeric',
            hour: '2-digit',
            minute: '2-digit',
        });
    }

    return (
        <div className="rounded-xl border border-border bg-card p-4 sm:p-6">
            <h3 className="mb-4 font-heading text-lg font-semibold">{t('history.heading')}</h3>

            {/* A phone has no room for five columns, and the old `overflow-x-auto` did not solve that —
                it only hid it behind a scrollbar nobody thinks to drag. Below `sm:` the same rows are
                cards, where the labels sit next to their values instead of in a header far to the left. */}
            <ul className="space-y-3 sm:hidden">
                {ordered.map((attempt) => (
                    <li
                        key={attempt.attemptId}
                        className="rounded-lg border border-border bg-muted/30 p-3"
                    >
                        <div className="flex items-center justify-between gap-2">
                            <span className="text-sm font-medium">
                                {t('history.attemptColumn')} {attempt.attemptNumber}
                            </span>
                            <ResultBadge passed={attempt.passed} />
                        </div>

                        <p className="mt-1 text-xs text-muted-foreground">
                            {formatDate(attempt.submittedAt)}
                        </p>

                        <div className="mt-2 flex items-center justify-between gap-2">
                            <span className="text-sm">
                                {t('status.score', {
                                    score: attempt.score,
                                    max: attempt.maxScore,
                                })}
                            </span>
                            {onReview && (
                                <ReviewButton onClick={() => onReview(attempt.attemptId)} />
                            )}
                        </div>
                    </li>
                ))}
            </ul>

            <div className="hidden sm:block">
                <table className="w-full text-sm">
                    <thead>
                        <tr className="border-b border-border text-left text-muted-foreground">
                            <th className="whitespace-nowrap pb-2 pr-4 font-medium">
                                {t('history.attemptColumn')}
                            </th>
                            <th className="whitespace-nowrap pb-2 pr-4 font-medium">
                                {t('common:general.date')}
                            </th>
                            <th className="whitespace-nowrap pb-2 pr-4 font-medium">
                                {t('history.score')}
                            </th>
                            <th className="whitespace-nowrap pb-2 pr-4 font-medium">
                                {t('history.result')}
                            </th>
                            {onReview && <th className="pb-2" />}
                        </tr>
                    </thead>
                    <tbody className="divide-y divide-border">
                        {ordered.map((attempt) => (
                            <tr key={attempt.attemptId}>
                                {/* The column already says "Attempt" — the cell only has to say which. */}
                                <td className="whitespace-nowrap py-3 pr-4">
                                    {attempt.attemptNumber}
                                </td>
                                <td className="whitespace-nowrap py-3 pr-4 text-muted-foreground">
                                    {formatDate(attempt.submittedAt)}
                                </td>
                                <td className="whitespace-nowrap py-3 pr-4">
                                    {t('status.score', {
                                        score: attempt.score,
                                        max: attempt.maxScore,
                                    })}
                                </td>
                                <td className="whitespace-nowrap py-3 pr-4">
                                    <ResultBadge passed={attempt.passed} />
                                </td>
                                {onReview && (
                                    <td className="whitespace-nowrap py-3 text-right">
                                        <ReviewButton onClick={() => onReview(attempt.attemptId)} />
                                    </td>
                                )}
                            </tr>
                        ))}
                    </tbody>
                </table>
            </div>
        </div>
    );
}

interface ResultBadgeProps {
    passed: boolean;
}

function ResultBadge({ passed }: ResultBadgeProps) {
    const { t } = useTranslation('testLesson');

    return (
        <span
            className={cn(
                'inline-flex shrink-0 items-center gap-1.5 rounded-full px-2.5 py-0.5 text-xs font-medium',
                passed ? 'bg-success/15 text-success' : 'bg-destructive/15 text-destructive',
            )}
        >
            {passed ? <CheckCircle2 className="size-3" /> : <XCircle className="size-3" />}
            {passed ? t('common:status.passed') : t('common:status.failed')}
        </span>
    );
}

interface ReviewButtonProps {
    onClick: () => void;
}

function ReviewButton({ onClick }: ReviewButtonProps) {
    const { t } = useTranslation('testLesson');

    return (
        <button
            type="button"
            onClick={onClick}
            className="inline-flex items-center gap-1.5 rounded-lg px-2.5 py-1 text-xs font-medium text-link transition-colors hover:bg-secondary hover:underline"
        >
            <Eye className="size-3.5" />
            {t('history.review')}
        </button>
    );
}
