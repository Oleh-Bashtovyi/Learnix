import type { UseFormRegister } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { FormCheckbox } from '@/components/common/form/FormCheckbox';
import { FormInput } from '@/components/common/form/FormInput';
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
            <FormInput
                label={
                    <span className="text-xs text-muted-foreground">{t('fieldCorrectAnswer')}</span>
                }
                placeholder={t('fieldCorrectAnswerPlaceholder')}
                error={qError?.textAnswer?.correctAnswer?.message}
                {...register(`questions.${qIdx}.textAnswer.correctAnswer`)}
            />
            <div className="flex items-center gap-6 text-sm">
                <FormCheckbox
                    label={t('fieldIgnoreCase')}
                    {...register(`questions.${qIdx}.textAnswer.ignoreCase`)}
                />
                <FormCheckbox
                    label={t('fieldAllowFuzzy')}
                    {...register(`questions.${qIdx}.textAnswer.allowFuzzy`)}
                />
            </div>
        </div>
    );
}
