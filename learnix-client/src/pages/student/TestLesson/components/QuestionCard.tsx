import { useTranslation } from 'react-i18next';
import type { QuestionDto, QuestionResultDto } from '@/types/lesson.types';
import { cn } from '@/utils/cn';
import { ChoiceQuestion } from './questions/ChoiceQuestion';
import { TextInputQuestion } from './questions/TextInputQuestion';

interface QuestionCardProps {
    question: QuestionDto;
    index: number;
    total: number;
    selectedOptions: number[];
    textValue: string;
    onOptionToggle: (optionOrder: number) => void;
    onTextChange: (value: string) => void;
    result?: QuestionResultDto;
    readonly?: boolean;
}

export function QuestionCard({
    question,
    index,
    total,
    selectedOptions,
    textValue,
    onOptionToggle,
    onTextChange,
    result,
    readonly = false,
}: QuestionCardProps) {
    const { t } = useTranslation('testLesson');
    const isCorrect = result?.isCorrect;
    const hasResult = result !== undefined;

    return (
        <div
            className={cn(
                'rounded-xl border p-6 transition-colors',
                hasResult
                    ? isCorrect
                        ? 'border-success/40 bg-success/5'
                        : 'border-destructive/40 bg-destructive/5'
                    : 'border-border bg-card',
            )}
        >
            {/* Question header */}
            <div className="mb-4 flex items-start justify-between gap-4">
                <div className="flex items-start gap-3">
                    <span className="mt-0.5 flex size-6 shrink-0 items-center justify-center rounded-full bg-secondary text-xs font-semibold text-muted-foreground">
                        {index + 1}
                    </span>
                    <p className="font-medium leading-relaxed">{question.text}</p>
                </div>
                {hasResult && (
                    <span
                        className={cn(
                            'shrink-0 rounded-full px-2.5 py-0.5 text-xs font-medium',
                            isCorrect
                                ? 'bg-success/15 text-success'
                                : 'bg-destructive/15 text-destructive',
                        )}
                    >
                        {isCorrect ? t('results.correct') : t('results.incorrect')}
                    </span>
                )}
            </div>

            {/* Subtext */}
            <p className="mb-3 ml-9 text-xs text-muted-foreground">
                {t('form.questionOf', { current: index + 1, total })}
                {!hasResult && question.type === 'MultipleChoice' && ` ${t('form.selectAll')}`}
                {!hasResult && question.type === 'SingleChoice' && ` ${t('form.selectOne')}`}
            </p>

            {/* Choice options */}
            {(question.type === 'SingleChoice' || question.type === 'MultipleChoice') &&
                question.options && (
                    <ChoiceQuestion
                        type={question.type}
                        options={question.options}
                        selectedOptions={selectedOptions}
                        onOptionToggle={onOptionToggle}
                        result={result}
                        readonly={readonly}
                    />
                )}

            {/* Text input */}
            {question.type === 'TextInput' && (
                <TextInputQuestion
                    textValue={textValue}
                    onTextChange={onTextChange}
                    result={result}
                    readonly={readonly}
                />
            )}
        </div>
    );
}
