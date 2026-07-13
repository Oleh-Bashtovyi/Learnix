import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import { CheckCircle2, XCircle } from 'lucide-react';
import { APP_ROUTES } from '@/routes/paths';
import type { GetTestLessonDto, SubmitAttemptResponse } from '@/types/lesson.types';
import { cn } from '@/utils/cn';
import { QuestionCard } from './QuestionCard';

interface AnswerState {
    selectedOptions: number[];
    textValue: string;
}

interface TestResultsProps {
    result: SubmitAttemptResponse;
    test: GetTestLessonDto;
    courseId: string;
    lessonId: string;
    submittedAnswers: Record<number, AnswerState>;
    onRetake: () => void;
    canRetake: boolean;
}

export function TestResults({
    result,
    test,
    courseId,
    lessonId,
    submittedAnswers,
    onRetake,
    canRetake,
}: TestResultsProps) {
    const { t } = useTranslation('testLesson');
    const percentage = result.maxScore > 0 ? Math.round((result.score / result.maxScore) * 100) : 0;

    return (
        <div className="space-y-8">
            {/* Score card */}
            <div
                className={cn(
                    'rounded-xl border p-5 text-center sm:p-6',
                    result.passed
                        ? 'border-success/30 bg-success/10'
                        : 'border-destructive/30 bg-destructive/10',
                )}
            >
                <div className="mb-3 flex justify-center">
                    {result.passed ? (
                        <CheckCircle2 className="size-12 text-success" />
                    ) : (
                        <XCircle className="size-12 text-destructive" />
                    )}
                </div>
                <h2 className="mb-1 font-heading text-2xl font-bold">{t('results.heading')}</h2>
                <p className="mb-4 text-sm text-muted-foreground">
                    {result.passed ? t('common:status.passed') : t('common:status.failed')}
                </p>
                <div className="mb-1 text-3xl font-bold">
                    {t('status.score', { score: result.score, max: result.maxScore })}
                </div>
                <p className="text-base text-muted-foreground">{percentage}%</p>
            </div>

            {/* Reviewed questions — show what the student actually selected. The instructor can turn
                this off entirely (ScoreOnly), in which case the backend sends no results at all. */}
            {result.questionResults.length === 0 ? (
                <p className="rounded-lg border border-border bg-muted/40 p-4 text-sm text-muted-foreground">
                    {t('results.reviewDisabled')}
                </p>
            ) : (
                <div>
                    <h3 className="mb-4 font-heading text-lg font-semibold">
                        {t('results.reviewHeading')}
                    </h3>
                    <div className="space-y-4">
                        {test.questions
                            .slice()
                            .sort((a, b) => a.order - b.order)
                            .map((question, idx) => {
                                const questionResult = result.questionResults.find(
                                    (qr) => qr.questionOrder === question.order,
                                );
                                const ans = submittedAnswers[question.order] ?? {
                                    selectedOptions: [],
                                    textValue: '',
                                };
                                return (
                                    <QuestionCard
                                        key={question.order}
                                        question={question}
                                        index={idx}
                                        total={test.questions.length}
                                        selectedOptions={ans.selectedOptions}
                                        textValue={ans.textValue}
                                        onOptionToggle={() => {}}
                                        onTextChange={() => {}}
                                        result={questionResult}
                                        readonly
                                    />
                                );
                            })}
                    </div>
                </div>
            )}

            {/* Actions */}
            <div className="flex flex-col gap-3 sm:flex-row sm:flex-wrap">
                <Link
                    to={APP_ROUTES.student.learnLesson(courseId, lessonId)}
                    className="flex justify-center rounded-lg border border-border px-5 py-2.5 text-sm font-medium transition-colors hover:bg-secondary sm:w-auto"
                >
                    {t('results.returnToLesson')}
                </Link>
                {canRetake && (
                    <button
                        type="button"
                        onClick={onRetake}
                        className="flex justify-center rounded-lg bg-primary px-5 py-2.5 text-sm font-medium text-primary-foreground transition-colors hover:bg-primary/90 sm:w-auto"
                    >
                        {t('results.retakeTest')}
                    </button>
                )}
            </div>
        </div>
    );
}
