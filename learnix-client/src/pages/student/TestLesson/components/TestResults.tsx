import { Link } from 'react-router-dom';
import { CheckCircle2, XCircle } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { cn } from '@/utils/cn';
import type { SubmitAttemptResponse, GetTestLessonDto } from '@/types/lesson.types';
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
                    'rounded-xl border p-8 text-center',
                    result.passed
                        ? 'border-success/30 bg-success/10'
                        : 'border-destructive/30 bg-destructive/10',
                )}
            >
                <div className="mb-4 flex justify-center">
                    {result.passed ? (
                        <CheckCircle2 className="h-16 w-16 text-success" />
                    ) : (
                        <XCircle className="h-16 w-16 text-destructive" />
                    )}
                </div>
                <h2 className="mb-2 font-heading text-2xl font-bold">{t('results.heading')}</h2>
                <p className="mb-4 text-muted-foreground">
                    {result.passed ? t('status.passed') : t('status.failed')}
                </p>
                <div className="mb-2 text-4xl font-bold">
                    {t('status.score', { score: result.score, max: result.maxScore })}
                </div>
                <p className="text-lg text-muted-foreground">{percentage}%</p>
            </div>

            {/* Reviewed questions — show what the student actually selected */}
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

            {/* Actions */}
            <div className="flex flex-wrap gap-3">
                <Link
                    to={`/courses/${courseId}/learn/${lessonId}`}
                    className="rounded-lg border border-border px-5 py-2.5 text-sm font-medium transition-colors hover:bg-secondary"
                >
                    {t('results.returnToLesson')}
                </Link>
                {canRetake && (
                    <button
                        type="button"
                        onClick={onRetake}
                        className="rounded-lg bg-primary px-5 py-2.5 text-sm font-medium text-primary-foreground transition-colors hover:bg-primary/90"
                    >
                        {t('results.retakeTest')}
                    </button>
                )}
            </div>
        </div>
    );
}
