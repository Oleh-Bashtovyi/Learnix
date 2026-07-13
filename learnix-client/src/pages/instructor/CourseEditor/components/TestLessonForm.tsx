import { useEffect } from 'react';
import { Controller, FormProvider, useFieldArray, useForm, useWatch } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { zodResolver } from '@hookform/resolvers/zod';
import { FormInput } from '@/components/common/form/FormInput';
import { FormSelect } from '@/components/common/form/FormSelect';
import { FormTextarea } from '@/components/common/form/FormTextarea';
import { LESSON_LIMITS, REVIEW_MODE_ORDER } from '@/const/lesson.constants';
import { TestReviewMode } from '@/enums/lesson.enums';
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
    const form = useForm<TestLessonFormData>({
        resolver: zodResolver(testLessonSchema),
        defaultValues: {
            title: lesson?.title ?? '',
            description: lesson?.description ?? '',
            passingThreshold: lesson?.passingThreshold ?? 70,
            reviewMode: lesson?.reviewMode ?? TestReviewMode.FullReview,
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

    const {
        register,
        handleSubmit,
        control,
        watch,
        setValue,
        formState: { errors, isDirty },
    } = form;

    useEffect(() => {
        onDirtyChange?.(isDirty);
    }, [isDirty, onDirtyChange]);

    const reviewMode = useWatch({ control, name: 'reviewMode' });

    const {
        fields: questionFields,
        append: addQuestion,
        remove: removeQuestion,
    } = useFieldArray({
        control,
        name: 'questions',
    });

    return (
        // FormProvider so the char counters inside the fields can read the live field values.
        <FormProvider {...form}>
            <form onSubmit={handleSubmit(onSubmit)} className="space-y-5">
                <FormInput
                    variant="card"
                    label={t('fieldTitle')}
                    maxLength={LESSON_LIMITS.TITLE_MAX}
                    showCharLimit
                    error={errors.title?.message}
                    {...register('title')}
                />

                <FormTextarea
                    variant="card"
                    label={t('fieldDescription')}
                    rows={2}
                    maxLength={LESSON_LIMITS.DESCRIPTION_MAX}
                    showCharLimit
                    error={errors.description?.message}
                    {...register('description')}
                />

                <div className="grid grid-cols-3 items-end gap-4">
                    <FormInput
                        variant="card"
                        label={t('fieldPassingThreshold')}
                        type="number"
                        min={LESSON_LIMITS.PASSING_THRESHOLD_MIN}
                        max={LESSON_LIMITS.PASSING_THRESHOLD_MAX}
                        error={errors.passingThreshold?.message}
                        {...register('passingThreshold', { valueAsNumber: true })}
                    />
                    <FormInput
                        variant="card"
                        label={t('fieldAttemptLimit')}
                        type="number"
                        min={LESSON_LIMITS.ATTEMPT_LIMIT_MIN}
                        placeholder="∞"
                        error={errors.attemptLimit?.message}
                        {...register('attemptLimit', { valueAsNumber: true })}
                    />
                    <FormInput
                        variant="card"
                        label={t('fieldCooldown')}
                        type="number"
                        min={LESSON_LIMITS.COOLDOWN_MINUTES_MIN}
                        error={errors.cooldownMinutes?.message}
                        {...register('cooldownMinutes', { valueAsNumber: true })}
                    />
                </div>

                {/* What a student may see back after submitting. It governs the results screen, the
                    review of past attempts and the AI tutor alike — the three ways an answer can get
                    out — so the choice here is the whole policy, not a display preference. */}
                <Controller
                    name="reviewMode"
                    control={control}
                    render={({ field }) => (
                        <FormSelect
                            variant="card"
                            label={t('fieldReviewMode')}
                            value={field.value}
                            onValueChange={field.onChange}
                            error={errors.reviewMode?.message}
                            options={REVIEW_MODE_ORDER.map((mode) => ({
                                value: mode,
                                label: t(`reviewMode.${mode}`),
                            }))}
                        />
                    )}
                />
                <p className="-mt-2 text-xs text-muted-foreground">
                    {t(`reviewModeHint.${reviewMode}`)}
                </p>

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
        </FormProvider>
    );
}
