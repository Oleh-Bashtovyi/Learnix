import { useTranslation } from 'react-i18next';
import type { GetTestLessonDto } from '@/types/lesson.types';
import type { AnswerState } from '../hooks/useTestDraft';
import { QuestionCard } from './QuestionCard';

interface QuizFormProps {
    test: GetTestLessonDto;
    answers: Record<number, AnswerState>;
    isStartError: boolean;
    isStartPending: boolean;
    onOptionToggle: (questionOrder: number, optionOrder: number, type: string) => void;
    onTextChange: (questionOrder: number, value: string) => void;
    onRetryStart: () => void;
}

export function QuizForm({
    test,
    answers,
    isStartError,
    isStartPending,
    onOptionToggle,
    onTextChange,
    onRetryStart,
}: QuizFormProps) {
    const { t } = useTranslation('testLesson');

    return (
        <div className="space-y-4">
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

            {/* Progress and submit now live in the sticky header. What stays here is the failure to
                open the attempt at all — that is not an action to keep within reach, it is an error
                about this form, and it belongs beside the form it broke. */}
            {isStartError && (
                <div className="flex flex-col items-end gap-2 pt-4">
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
            )}
        </div>
    );
}
