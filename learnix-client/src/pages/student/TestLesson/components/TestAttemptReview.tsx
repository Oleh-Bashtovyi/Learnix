import { useTranslation } from 'react-i18next';
import { QueryError } from '@/components/common/system/QueryError';
import { LoadingSpinner } from '@/components/common/ui/LoadingSpinner';
import {
    Dialog,
    DialogContent,
    DialogDescription,
    DialogHeader,
    DialogTitle,
} from '@/components/ui/dialog';
import { useTestAttemptReview } from '@/hooks/lesson/useTestAttemptReview';
import type { QuestionDto, QuestionResultDto, ReviewedQuestionDto } from '@/types/lesson.types';
import { QuestionCard } from './QuestionCard';

interface TestAttemptReviewProps {
    courseId: string;
    lessonId: string;
    attemptId: string;
    onClose: () => void;
}

/**
 * A past attempt, replayed. The answers were persisted from the very first submission — this is what
 * was missing to read them back.
 *
 * It renders through the same QuestionCard the live test and the results screen use, so a reviewed
 * question cannot drift from the question the student actually answered. The reviewed shape is mapped
 * onto that component's props here, and the nulls are load-bearing: a withheld verdict arrives as
 * null, never as false, and QuestionCard paints null as "not disclosed" rather than as "wrong".
 */
export function TestAttemptReview({
    courseId,
    lessonId,
    attemptId,
    onClose,
}: TestAttemptReviewProps) {
    const { t } = useTranslation('testLesson');
    const { data, isLoading, isError, refetch } = useTestAttemptReview(
        courseId,
        lessonId,
        attemptId,
    );

    return (
        <Dialog open onOpenChange={(open) => !open && onClose()}>
            {/* The review used to unfold in place, below the history table — far enough down the page
                that clicking the button looked like it had done nothing. A dialog is the honest shape
                for it: it is a detour from the page, not a part of it. */}
            <DialogContent className="flex max-h-[85vh] max-w-2xl flex-col">
                <DialogHeader>
                    <DialogTitle>
                        {data
                            ? t('review.headingWithNumber', { n: data.attemptNumber })
                            : t('review.heading')}
                    </DialogTitle>
                    {data && (
                        <DialogDescription>
                            {t('status.score', { score: data.score, max: data.maxScore })} —{' '}
                            {data.passed ? t('common:status.passed') : t('common:status.failed')}
                        </DialogDescription>
                    )}
                </DialogHeader>

                {/* The questions scroll; the header and its score stay put. */}
                <div className="-mx-1 flex-1 overflow-y-auto px-1">
                    {isLoading && (
                        <div className="flex justify-center py-10">
                            <LoadingSpinner />
                        </div>
                    )}

                    {isError && (
                        <QueryError
                            message={t('review.error')}
                            onRetry={refetch}
                            retryLabel={t('common:actions.tryAgain')}
                        />
                    )}

                    {data && data.questions.length === 0 && (
                        <p className="rounded-lg border border-border bg-muted/40 p-4 text-sm text-muted-foreground">
                            {t('results.reviewDisabled')}
                        </p>
                    )}

                    {data && data.questions.length > 0 && (
                        <div className="space-y-4">
                            {data.questions.map((reviewed, idx) => (
                                <QuestionCard
                                    key={reviewed.order}
                                    question={toQuestion(reviewed)}
                                    index={idx}
                                    total={data.questions.length}
                                    selectedOptions={reviewed.studentSelectedOptionOrders ?? []}
                                    textValue={reviewed.studentTextAnswer ?? ''}
                                    onOptionToggle={() => {}}
                                    onTextChange={() => {}}
                                    result={toResult(reviewed)}
                                    readonly
                                />
                            ))}
                        </div>
                    )}
                </div>
            </DialogContent>
        </Dialog>
    );
}

function toQuestion(reviewed: ReviewedQuestionDto): QuestionDto {
    return {
        text: reviewed.text,
        type: reviewed.type,
        order: reviewed.order,
        options: reviewed.options?.map((o) => ({ text: o.text, order: o.order })) ?? null,
    };
}

function toResult(reviewed: ReviewedQuestionDto): QuestionResultDto {
    // `isCorrect` on an option is null unless the mode discloses correct answers, so an all-null set
    // has to stay null here — an empty array would read as "no option was correct".
    const correctOptionOrders =
        reviewed.options?.some((o) => o.isCorrect !== null) === true
            ? reviewed.options.filter((o) => o.isCorrect === true).map((o) => o.order)
            : null;

    return {
        questionOrder: reviewed.order,
        isCorrect: reviewed.isCorrect,
        correctOptionOrders,
        correctTextAnswer: reviewed.correctTextAnswer,
    };
}
