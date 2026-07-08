import { useEffect } from 'react';
import { useFieldArray, useForm } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { zodResolver } from '@hookform/resolvers/zod';
import { FormInput } from '@/components/common/form/FormInput';
import { FormTextarea } from '@/components/common/form/FormTextarea';
import { LESSON_LIMITS } from '@/const/lesson.constants';
import { type TestLessonFormData, testLessonSchema } from '@/schemas/lesson.schema';
import type { CourseForEditLessonDto } from '@/types/course.types';
import { QuestionEditor } from './questions/QuestionEditor';

interface Props {
    lesson?: CourseForEditLessonDto;
    isPending: boolean;
    onSubmit: (data: TestLessonFormData) => void;
    onCancel: () => void;
    onDirtyChange?: (isDirty: boolean) => void;
}

export function TestLessonForm({ lesson, isPending, onSubmit, onCancel, onDirtyChange }: Props) {
    const { t } = useTranslation('instructor');
    const {
        register,
        handleSubmit,
        control,
        watch,
        setValue,
        formState: { errors, isDirty },
    } = useForm<TestLessonFormData>({
        resolver: zodResolver(testLessonSchema),
        defaultValues: {
            title: lesson?.title ?? '',
            description: lesson?.description ?? '',
            passingThreshold: lesson?.passingThreshold ?? 70,
            attemptLimit: lesson?.attemptLimit ?? undefined,
            cooldownMinutes: lesson?.cooldownMinutes ?? undefined,
            questions: lesson?.questions?.map((q) => ({
                text: q.text,
                type: q.type,
                options: q.options.map((o) => ({ text: o.text, isCorrect: o.isCorrect })),
                textAnswer:
                    q.type === 'TextInput'
                        ? {
                              correctAnswer: q.correctAnswer ?? '',
                              ignoreCase: q.ignoreCase ?? false,
                              allowFuzzy: q.allowFuzzy ?? false,
                          }
                        : undefined,
            })) ?? [
                {
                    text: '',
                    type: 'SingleChoice',
                    options: [
                        { text: '', isCorrect: false },
                        { text: '', isCorrect: false },
                    ],
                },
            ],
        },
    });

    useEffect(() => {
        onDirtyChange?.(isDirty);
    }, [isDirty, onDirtyChange]);

    const {
        fields: questionFields,
        append: addQuestion,
        remove: removeQuestion,
    } = useFieldArray({
        control,
        name: 'questions',
    });

    return (
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-5">
            <FormInput
                label={t('fieldTitle')}
                error={errors.title?.message}
                {...register('title')}
            />

            <FormTextarea
                label={t('fieldDescription')}
                rows={2}
                error={errors.description?.message}
                {...register('description')}
            />

            <div className="grid grid-cols-3 items-end gap-4">
                <FormInput
                    label={t('fieldPassingThreshold')}
                    type="number"
                    min={LESSON_LIMITS.PASSING_THRESHOLD_MIN}
                    max={LESSON_LIMITS.PASSING_THRESHOLD_MAX}
                    error={errors.passingThreshold?.message}
                    {...register('passingThreshold', { valueAsNumber: true })}
                />
                <FormInput
                    label={t('fieldAttemptLimit')}
                    type="number"
                    min={LESSON_LIMITS.ATTEMPT_LIMIT_MIN}
                    placeholder="∞"
                    error={errors.attemptLimit?.message}
                    {...register('attemptLimit', { valueAsNumber: true })}
                />
                <FormInput
                    label={t('fieldCooldown')}
                    type="number"
                    min={LESSON_LIMITS.COOLDOWN_MINUTES_MIN}
                    error={errors.cooldownMinutes?.message}
                    {...register('cooldownMinutes', { valueAsNumber: true })}
                />
            </div>

            {/* Questions */}
            <div className="space-y-4">
                <div className="flex items-center justify-between">
                    <h4 className="font-medium text-foreground">Questions</h4>
                    <button
                        type="button"
                        onClick={() =>
                            addQuestion({
                                text: '',
                                type: 'SingleChoice',
                                options: [
                                    { text: '', isCorrect: false },
                                    { text: '', isCorrect: false },
                                ],
                            })
                        }
                        className="text-sm text-primary hover:underline"
                    >
                        {t('btnAddQuestion')}
                    </button>
                </div>
                {errors.questions?.root && (
                    <p className="text-xs text-destructive">{errors.questions.root.message}</p>
                )}

                {questionFields.map((qField, qIdx) => (
                    <QuestionEditor
                        key={qField.id}
                        qIdx={qIdx}
                        register={register}
                        control={control}
                        watch={watch}
                        setValue={setValue}
                        errors={errors}
                        onRemove={() => removeQuestion(qIdx)}
                    />
                ))}
            </div>

            <div className="flex justify-end gap-2 pt-2">
                <button
                    type="button"
                    onClick={onCancel}
                    className="rounded-lg border border-border px-4 py-2 text-sm hover:bg-secondary"
                >
                    {t('common:actions.cancel')}
                </button>
                <button
                    type="submit"
                    disabled={isPending || !isDirty}
                    className="rounded-lg bg-primary px-4 py-2 text-sm font-medium text-primary-foreground hover:bg-primary/90 disabled:cursor-not-allowed disabled:opacity-60"
                >
                    {isPending ? '...' : t('btnSaveLesson')}
                </button>
            </div>
        </form>
    );
}
