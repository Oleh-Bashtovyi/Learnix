import { useTranslation } from 'react-i18next';
import { ChoiceIndicator } from '@/components/common/form/ChoiceIndicator';
import type { QuestionOptionDto, QuestionResultDto } from '@/types/lesson.types';
import { cn } from '@/utils/cn';

interface ChoiceQuestionProps {
    type: 'SingleChoice' | 'MultipleChoice';
    options: QuestionOptionDto[];
    selectedOptions: number[];
    onOptionToggle: (optionOrder: number) => void;
    result?: QuestionResultDto;
    readonly?: boolean;
}

export function ChoiceQuestion({
    type,
    options,
    selectedOptions,
    onOptionToggle,
    result,
    readonly = false,
}: ChoiceQuestionProps) {
    const { t } = useTranslation('testLesson');
    const correctOrders = new Set(result?.correctOptionOrders ?? []);
    const hasResult = result !== undefined;
    /** Only FullReview sends the correct option orders; below it they are withheld, not absent. */
    const knowsCorrectOptions = result?.correctOptionOrders != null;
    /** The question-level verdict, or null when the mode withholds correctness too. */
    const questionVerdict = result?.isCorrect ?? null;

    return (
        <ul className="space-y-2 sm:ml-9">
            {options
                .slice()
                .sort((a, b) => a.order - b.order)
                .map((option) => {
                    const isSelected = selectedOptions.includes(option.order);
                    const isCorrectOption = correctOrders.has(option.order);
                    const inputType = type === 'SingleChoice' ? 'radio' : 'checkbox';

                    // How the review is painted depends on how much the test's review mode disclosed.
                    //
                    // FullReview marks each option — the student's pick in green or red, the right
                    // answer outlined even if they missed it. Below that, `correctOptionOrders` comes
                    // back null: the platform genuinely does not tell us which option was right, and
                    // colouring the unpicked ones red would be inventing an answer. So we mark only
                    // what the student chose, tinted by the question's own verdict when we have one,
                    // and left neutral when we do not.
                    const reviewStyle = !hasResult
                        ? null
                        : knowsCorrectOptions
                          ? isSelected && isCorrectOption
                              ? 'border-success bg-success/10 text-success'
                              : isSelected && !isCorrectOption
                                ? 'border-destructive bg-destructive/10 text-destructive'
                                : isCorrectOption
                                  ? 'border-success/50 bg-success/5'
                                  : 'border-border opacity-60'
                          : !isSelected
                            ? 'border-border opacity-60'
                            : questionVerdict === true
                              ? 'border-success bg-success/10 text-success'
                              : questionVerdict === false
                                ? 'border-destructive bg-destructive/10 text-destructive'
                                : 'border-primary bg-primary/5';

                    // The control says the same thing the card does.
                    const tone: 'default' | 'success' | 'destructive' = !hasResult
                        ? 'default'
                        : knowsCorrectOptions
                          ? isCorrectOption
                              ? 'success'
                              : 'destructive'
                          : isSelected && questionVerdict === true
                            ? 'success'
                            : isSelected && questionVerdict === false
                              ? 'destructive'
                              : 'default';

                    return (
                        <li key={option.order}>
                            <label
                                className={cn(
                                    'group flex cursor-pointer items-center gap-3 rounded-lg border px-4 py-3 text-sm transition-colors',
                                    readonly && 'cursor-default',
                                    !hasResult && isSelected
                                        ? 'border-primary bg-primary/5'
                                        : !hasResult
                                          ? 'border-border hover:border-primary/50 hover:bg-secondary'
                                          : '',
                                    reviewStyle,
                                )}
                            >
                                {/* Real input, hidden: the browser's own control is a white disc in the
                                    dark theme. Keyboard and screen readers still get a genuine radio. */}
                                <input
                                    type={inputType}
                                    disabled={readonly}
                                    checked={isSelected}
                                    onChange={() => onOptionToggle(option.order)}
                                    className="sr-only"
                                />
                                <ChoiceIndicator
                                    type={inputType}
                                    checked={isSelected}
                                    tone={tone}
                                />
                                <span>{option.text}</span>
                                {hasResult && isCorrectOption && (
                                    <span className="ml-auto text-xs font-medium text-success">
                                        {t('results.correctAnswer')}
                                    </span>
                                )}
                            </label>
                        </li>
                    );
                })}
        </ul>
    );
}
