import { CheckCircle2, XCircle } from 'lucide-react';
import { cn } from '@/utils/cn';
import type { TestAttemptSummaryDto } from '@/types/lesson.types';
import { TEST_LESSON } from '@/const/localization/testLesson';

interface TestAttemptHistoryProps {
    attempts: TestAttemptSummaryDto[];
}

export function TestAttemptHistory({ attempts }: TestAttemptHistoryProps) {
    if (attempts.length === 0) {
        return (
            <div className="rounded-xl border border-border bg-card p-6">
                <h3 className="mb-4 font-heading text-lg font-semibold">
                    {TEST_LESSON.HISTORY.heading}
                </h3>
                <p className="text-sm text-muted-foreground">{TEST_LESSON.HISTORY.noAttempts}</p>
            </div>
        );
    }

    return (
        <div className="rounded-xl border border-border bg-card p-6">
            <h3 className="mb-4 font-heading text-lg font-semibold">
                {TEST_LESSON.HISTORY.heading}
            </h3>
            <div className="overflow-x-auto">
                <table className="w-full text-sm">
                    <thead>
                        <tr className="border-b border-border text-left text-muted-foreground">
                            <th className="pb-2 pr-4 font-medium">
                                {TEST_LESSON.HISTORY.attemptColumn}
                            </th>
                            <th className="pb-2 pr-4 font-medium">{TEST_LESSON.HISTORY.date}</th>
                            <th className="pb-2 pr-4 font-medium">{TEST_LESSON.HISTORY.score}</th>
                            <th className="pb-2 font-medium">{TEST_LESSON.HISTORY.result}</th>
                        </tr>
                    </thead>
                    <tbody className="divide-y divide-border">
                        {attempts
                            .slice()
                            .sort((a, b) => b.attemptNumber - a.attemptNumber)
                            .map((attempt) => (
                                <tr key={attempt.attemptId} className="py-2">
                                    <td className="py-3 pr-4">
                                        {TEST_LESSON.HISTORY.attempt(attempt.attemptNumber)}
                                    </td>
                                    <td className="py-3 pr-4 text-muted-foreground">
                                        {new Date(attempt.submittedAt).toLocaleDateString(
                                            undefined,
                                            {
                                                year: 'numeric',
                                                month: 'short',
                                                day: 'numeric',
                                                hour: '2-digit',
                                                minute: '2-digit',
                                            },
                                        )}
                                    </td>
                                    <td className="py-3 pr-4">
                                        {TEST_LESSON.STATUS.score(attempt.score, attempt.maxScore)}
                                    </td>
                                    <td className="py-3">
                                        <span
                                            className={cn(
                                                'inline-flex items-center gap-1.5 rounded-full px-2.5 py-0.5 text-xs font-medium',
                                                attempt.passed
                                                    ? 'bg-success/15 text-success'
                                                    : 'bg-destructive/15 text-destructive',
                                            )}
                                        >
                                            {attempt.passed ? (
                                                <CheckCircle2 className="h-3 w-3" />
                                            ) : (
                                                <XCircle className="h-3 w-3" />
                                            )}
                                            {attempt.passed
                                                ? TEST_LESSON.STATUS.passed
                                                : TEST_LESSON.STATUS.failed}
                                        </span>
                                    </td>
                                </tr>
                            ))}
                    </tbody>
                </table>
            </div>
        </div>
    );
}
