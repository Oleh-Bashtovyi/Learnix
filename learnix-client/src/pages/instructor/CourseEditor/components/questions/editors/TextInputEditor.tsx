import type { UseFormRegister } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import type { TestLessonFormData } from '@/schemas/lesson.schema';

interface TextInputEditorProps {
    qIdx: number;
    register: UseFormRegister<TestLessonFormData>;
    qError?: {
        textAnswer?: {
            correctAnswer?: { message?: string };
        };
    };
}

export function TextInputEditor({ qIdx, register, qError }: TextInputEditorProps) {
    const { t } = useTranslation('instructor');

    return (
        <div className="space-y-3 pt-2">
            <div>
                <label className="mb-1 block text-xs text-muted-foreground">
                    {t('fieldCorrectAnswer')}
                </label>
                <input
                    {...register(`questions.${qIdx}.textAnswer.correctAnswer`)}
                    placeholder={t('fieldCorrectAnswerPlaceholder')}
                    className="w-full rounded border border-input bg-background px-3 py-1.5 text-sm focus:outline-none focus:ring-1 focus:ring-ring"
                />
                {qError?.textAnswer?.correctAnswer && (
                    <p className="mt-1 text-xs text-destructive">
                        {qError.textAnswer.correctAnswer.message}
                    </p>
                )}
            </div>
            <div className="flex items-center gap-4 text-sm">
                <label className="flex items-center gap-2">
                    <input
                        type="checkbox"
                        {...register(`questions.${qIdx}.textAnswer.ignoreCase`)}
                        className="accent-primary"
                    />
                    {t('fieldIgnoreCase')}
                </label>
                <label className="flex items-center gap-2">
                    <input
                        type="checkbox"
                        {...register(`questions.${qIdx}.textAnswer.allowFuzzy`)}
                        className="accent-primary"
                    />
                    {t('fieldAllowFuzzy')}
                </label>
            </div>
        </div>
    );
}
