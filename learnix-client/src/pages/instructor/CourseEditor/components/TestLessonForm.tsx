import { useEffect } from 'react';
import { useFieldArray, useForm } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { zodResolver } from '@hookform/resolvers/zod';
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
            <div>
                <label className="mb-1 block text-sm font-medium">{t('fieldTitle')}</label>
                <input
                    {...register('title')}
                    className="w-full rounded-lg border border-input bg-background px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-ring"
                />
                {errors.title && (
                    <p className="mt-1 text-xs text-destructive">{errors.title.message}</p>
                )}
            </div>

            <div>
                <label className="mb-1 block text-sm font-medium">{t('fieldDescription')}</label>
                <textarea
                    {...register('description')}
                    rows={2}
                    className="w-full resize-none rounded-lg border border-input bg-background px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-ring"
                />
            </div>

            <div className="grid grid-cols-3 items-end gap-4">
                <div>
                    <label className="mb-1 block text-sm font-medium">
                        {t('fieldPassingThreshold')}
                    </label>
                    <input
                        {...register('passingThreshold', { valueAsNumber: true })}
                        type="number"
                        min={LESSON_LIMITS.PASSING_THRESHOLD_MIN}
                        max={LESSON_LIMITS.PASSING_THRESHOLD_MAX}
                        className="w-full rounded-lg border border-input bg-background px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-ring"
                    />
                    {errors.passingThreshold && (
                        <p className="mt-1 text-xs text-destructive">
                            {errors.passingThreshold.message}
                        </p>
                    )}
                </div>
                <div>
                    <label className="mb-1 block text-sm font-medium">
                        {t('fieldAttemptLimit')}
                    </label>
                    <input
                        {...register('attemptLimit', { valueAsNumber: true })}
                        type="number"
                        min={LESSON_LIMITS.ATTEMPT_LIMIT_MIN}
                        placeholder="∞"
                        className="w-full rounded-lg border border-input bg-background px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-ring"
                    />
                </div>
                <div>
                    <label className="mb-1 block text-sm font-medium">{t('fieldCooldown')}</label>
                    <input
                        {...register('cooldownMinutes', { valueAsNumber: true })}
                        type="number"
                        min={LESSON_LIMITS.COOLDOWN_MINUTES_MIN}
                        className="w-full rounded-lg border border-input bg-background px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-ring"
                    />
                </div>
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
                    {t('btnCancel')}
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
