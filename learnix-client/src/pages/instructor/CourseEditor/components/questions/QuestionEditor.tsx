import type {
    Control,
    FieldErrors,
    UseFormRegister,
    UseFormSetValue,
    UseFormWatch,
} from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { Trash2 } from 'lucide-react';
import type { TestLessonFormData } from '@/schemas/lesson.schema';
import { ChoiceEditor } from './editors/ChoiceEditor';
import { TextInputEditor } from './editors/TextInputEditor';

export type QuestionEditorProps = {
    qIdx: number;
    register: UseFormRegister<TestLessonFormData>;
    control: Control<TestLessonFormData>;
    watch: UseFormWatch<TestLessonFormData>;
    setValue: UseFormSetValue<TestLessonFormData>;
    errors: FieldErrors<TestLessonFormData>;
    onRemove: () => void;
};

export function QuestionEditor({
    qIdx,
    register,
    control,
    watch,
    setValue,
    errors,
    onRemove,
}: QuestionEditorProps) {
    const { t } = useTranslation('instructor');

    const qType = watch(`questions.${qIdx}.type`);

    type QuestionErrorExt = {
        text?: { message?: string };
        options?: { root?: { message?: string }; message?: string };
        textAnswer?: { correctAnswer?: { message?: string } };
    };
    const qError = (errors.questions || [])[qIdx] as unknown as QuestionErrorExt;

    return (
        <div className="space-y-3 rounded-lg border border-border p-4">
            <div className="space-y-2">
                <div className="flex items-center gap-1.5">
                    <input
                        {...register(`questions.${qIdx}.text` as const)}
                        placeholder={t('fieldQuestionText')}
                        className="flex-1 rounded-lg border border-input bg-background px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-ring"
                    />
                    <select
                        {...register(`questions.${qIdx}.type` as const)}
                        className="cursor-pointer appearance-none rounded-lg border border-input bg-background bg-[url('data:image/svg+xml;charset=utf-8,%3Csvg%20xmlns%3D%22http%3A%2F%2Fwww.w3.org%2F2000%2Fsvg%22%20fill%3D%22none%22%20viewBox%3D%220%200%2020%2020%22%20stroke%3D%22%236b7280%22%3E%3Cpath%20stroke-linecap%3D%22round%22%20stroke-linejoin%3D%22round%22%20stroke-width%3D%221.5%22%20d%3D%22M6%208l4%204%204-4%22%2F%3E%3C%2Fsvg%3E')] bg-[length:18px_18px] bg-[position:right_8px_center] bg-no-repeat py-2 pl-3 pr-10 text-sm focus:outline-none focus:ring-2 focus:ring-ring"
                    >
                        <option value="SingleChoice" className="bg-background py-1 text-foreground">
                            {t('questionTypeSingle')}
                        </option>
                        <option
                            value="MultipleChoice"
                            className="bg-background py-1 text-foreground"
                        >
                            {t('questionTypeMultiple')}
                        </option>
                        <option value="TextInput" className="bg-background py-1 text-foreground">
                            {t('questionTypeTextInput')}
                        </option>
                    </select>
                    <button
                        type="button"
                        onClick={onRemove}
                        className="flex size-9 shrink-0 items-center justify-center rounded-md text-muted-foreground transition-colors hover:bg-destructive/10 hover:text-destructive"
                        title={t('btnDeleteQuestion')}
                    >
                        <Trash2 size={16} />
                    </button>
                </div>
                {qError?.text && (
                    <p className="pl-1 text-xs text-destructive">{qError.text.message}</p>
                )}
            </div>

            <div className="space-y-2 pl-2">
                {qType === 'TextInput' ? (
                    <TextInputEditor qIdx={qIdx} register={register} qError={qError} />
                ) : (
                    <ChoiceEditor
                        qIdx={qIdx}
                        qType={qType}
                        register={register}
                        control={control}
                        watch={watch}
                        setValue={setValue}
                        qError={qError}
                    />
                )}
            </div>
        </div>
    );
}
