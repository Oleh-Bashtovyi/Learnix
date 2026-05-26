import { Link } from 'react-router-dom';
import { CheckCircle2, XCircle } from 'lucide-react';
import { cn } from '@/utils/cn';
import type { SubmitAttemptResponse, GetTestLessonDto } from '@/types/lesson.types';
import { QuestionCard } from './QuestionCard';
import { TEST_LESSON } from '@/const/localization/testLesson';

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
                <h2 className="mb-2 font-heading text-2xl font-bold">
                    {TEST_LESSON.RESULTS.heading}
                </h2>
                <p className="mb-4 text-muted-foreground">
                    {result.passed ? TEST_LESSON.RESULTS.passed : TEST_LESSON.RESULTS.failed}
                </p>
                <div className="mb-2 text-4xl font-bold">
                    {TEST_LESSON.STATUS.score(result.score, result.maxScore)}
                </div>
                <p className="text-lg text-muted-foreground">{percentage}%</p>
            </div>

            {/* Reviewed questions — show what the student actually selected */}
            <div>
                <h3 className="mb-4 font-heading text-lg font-semibold">
                    {TEST_LESSON.RESULTS.reviewHeading}
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
                    {TEST_LESSON.RESULTS.returnToLesson}
                </Link>
                {canRetake && (
                    <button
                        type="button"
                        onClick={onRetake}
                        className="rounded-lg bg-primary px-5 py-2.5 text-sm font-medium text-primary-foreground transition-colors hover:bg-primary/90"
                    >
                        {TEST_LESSON.RESULTS.retakeTest}
                    </button>
                )}
            </div>
        </div>
    );
}
