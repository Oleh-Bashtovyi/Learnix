import { useTranslation } from 'react-i18next';
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
        <div className="ml-9">
            <textarea
                value={textValue}
                onChange={(e) => onTextChange(e.target.value)}
                disabled={readonly}
                placeholder={t('form.textPlaceholder')}
                rows={3}
                className="w-full resize-none rounded-lg border border-border bg-background px-4 py-3 text-sm placeholder:text-muted-foreground focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary disabled:cursor-default disabled:opacity-70"
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
