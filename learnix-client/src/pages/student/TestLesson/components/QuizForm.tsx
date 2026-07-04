import { useTranslation } from 'react-i18next';
import type { GetTestLessonDto } from '@/types/lesson.types';
import { cn } from '@/utils/cn';
import type { AnswerState } from '../hooks/useTestDraft';
import { QuestionCard } from './QuestionCard';

interface QuizFormProps {
    test: GetTestLessonDto;
    answeredCount: number;
    totalQuestions: number;
    answers: Record<number, AnswerState>;
    attemptId: string | null;
    isStartError: boolean;
    isStartPending: boolean;
    isSubmitPending: boolean;
    onOptionToggle: (questionOrder: number, optionOrder: number, type: string) => void;
    onTextChange: (questionOrder: number, value: string) => void;
    onRetryStart: () => void;
    onSubmitClick: () => void;
}

export function QuizForm({
    test,
    answeredCount,
    totalQuestions,
    answers,
    attemptId,
    isStartError,
    isStartPending,
    isSubmitPending,
    onOptionToggle,
    onTextChange,
    onRetryStart,
    onSubmitClick,
}: QuizFormProps) {
    const { t } = useTranslation('testLesson');

    return (
        <div className="space-y-4">
            {/* Progress indicator */}
            {totalQuestions > 0 && (
                <div className="flex items-center justify-between text-xs text-muted-foreground">
                    <span>
                        {t('form.answeredOf', {
                            answered: answeredCount,
                            total: totalQuestions,
                        })}
                    </span>
                    <div className="h-1.5 w-48 overflow-hidden rounded-full bg-secondary">
                        <div
                            className="h-full rounded-full bg-primary transition-all"
                            style={{
                                width: `${totalQuestions > 0 ? (answeredCount / totalQuestions) * 100 : 0}%`,
                            }}
                        />
                    </div>
                </div>
            )}

            {test.questions
                .slice()
                .sort((a, b) => a.order - b.order)
                .map((question, idx) => (
                    <QuestionCard
                        key={question.order}
                        question={question}
                        index={idx}
                        total={test.questions.length}
                        selectedOptions={answers[question.order]?.selectedOptions ?? []}
                        textValue={answers[question.order]?.textValue ?? ''}
                        onOptionToggle={(optOrder) =>
                            onOptionToggle(question.order, optOrder, question.type)
                        }
                        onTextChange={(val) => onTextChange(question.order, val)}
                    />
                ))}

            <div className="flex justify-end pt-4">
                {isStartError ? (
                    <div className="flex flex-col items-end gap-2">
                        <p className="text-sm text-destructive">{t('form.startError')}</p>
                        <button
                            type="button"
                            onClick={onRetryStart}
                            disabled={isStartPending}
                            className="rounded-lg bg-secondary px-6 py-2.5 text-sm font-medium transition-colors hover:bg-secondary/80 disabled:opacity-50"
                        >
                            {isStartPending ? t('form.starting') : t('form.retry')}
                        </button>
                    </div>
                ) : (
                    <button
                        type="button"
                        onClick={onSubmitClick}
                        disabled={isSubmitPending || !attemptId}
                        className={cn(
                            'rounded-lg px-8 py-3 text-sm font-medium text-primary-foreground transition-colors',
                            isSubmitPending || !attemptId
                                ? 'cursor-not-allowed bg-primary/60'
                                : 'bg-primary hover:bg-primary/90',
                        )}
                    >
                        {isSubmitPending
                            ? t('form.submitting')
                            : !attemptId
                              ? t('form.starting')
                              : t('form.submitButton')}
                    </button>
                )}
            </div>
        </div>
    );
}
