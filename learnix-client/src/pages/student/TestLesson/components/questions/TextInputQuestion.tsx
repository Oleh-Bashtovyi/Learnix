import { useTranslation } from 'react-i18next';
import { FormTextarea } from '@/components/common/form/FormTextarea';
import { LESSON_LIMITS } from '@/const/lesson.constants';
import type { QuestionResultDto } from '@/types/lesson.types';

interface TextInputQuestionProps {
    textValue: string;
    onTextChange: (value: string) => void;
    result?: QuestionResultDto;
    readonly?: boolean;
}

export function TextInputQuestion({
    textValue,
    onTextChange,
    result,
    readonly = false,
}: TextInputQuestionProps) {
    const { t } = useTranslation('testLesson');
    const isCorrect = result?.isCorrect;
    const hasResult = result !== undefined;

    return (
        <div className="sm:ml-9">
            <FormTextarea
                value={textValue}
                onChange={(e) => onTextChange(e.target.value)}
                disabled={readonly}
                placeholder={t('form.textPlaceholder')}
                rows={3}
                maxLength={LESSON_LIMITS.TEXT_ANSWER_MAX}
            />
            {hasResult && !isCorrect && result?.correctTextAnswer && (
                <p className="mt-2 text-sm font-medium text-success">
                    {t('results.correctAnswer')}:{' '}
                    <span className="font-semibold">{result.correctTextAnswer}</span>
                </p>
            )}
        </div>
    );
}
