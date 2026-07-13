import { useEffect, useState } from 'react';
import type { KeyboardEvent } from 'react';
import { Controller, FormProvider, useForm, useWatch } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { zodResolver } from '@hookform/resolvers/zod';
import { X } from 'lucide-react';
import { FormInput } from '@/components/common/form/FormInput';
import { FormSelect } from '@/components/common/form/FormSelect';
import { FormTextarea } from '@/components/common/form/FormTextarea';
import { COURSE_LIMITS } from '@/const/course.constants';
import { useCategories } from '@/hooks/course/useCategories';
import { type CourseInfoFormData, courseInfoSchema } from '@/schemas/course.schema';
import type { CourseForEditDto } from '@/types/course.types';
import { CoverImageUploader } from './CoverImageUploader';

interface Props {
    course?: CourseForEditDto;
    isPending: boolean;
    onSubmit: (data: CourseInfoFormData) => void;
}

export function CourseInfoForm({ course, isPending, onSubmit }: Props) {
    const { t } = useTranslation('instructor');
    const { data: categories = [] } = useCategories();

    // `coverImageUrl` in the form means "a NEW cover to claim", never "the cover this course has".
    // The two are different things: the server hands back a public https URL for display, while the
    // command expects a temp blob path it can commit. Seeding the field with the display URL would
    // send it straight back, and the handler — which commits whenever the value differs from the
    // stored blob path — would try to claim an https URL as a blob. So it starts null, and stays
    // null unless the uploader puts a fresh blob path in it.
    const form = useForm<CourseInfoFormData>({
        resolver: zodResolver(courseInfoSchema),
        defaultValues: {
            title: course?.title ?? '',
            description: course?.description ?? '',
            categoryId: course?.categoryId ?? '',
            price: course?.price ?? 0,
            coverImageUrl: null,
            tags: course?.tags ?? [],
        },
    });

    const {
        register,
        handleSubmit,
        control,
        setValue,
        formState: { errors },
    } = form;

    // A blob path is a one-shot claim: committing it moves the blob out of temp-uploads and deletes
    // the original. Once the save lands, the server's cover URL changes — that is the signal that the
    // claim was spent, so drop it. Re-submitting it would fail with "file not found or expired".
    const serverCoverUrl = course?.coverImageUrl ?? null;
    useEffect(() => {
        setValue('coverImageUrl', null);
    }, [serverCoverUrl, setValue]);

    const tags = useWatch({ control, name: 'tags' }) ?? [];
    const [tagInput, setTagInput] = useState('');

    function addTag(e: KeyboardEvent<HTMLInputElement>) {
        if (e.key !== 'Enter' && e.key !== ',') return;
        e.preventDefault();
        const tag = tagInput.trim().toLowerCase();
        if (tag && !tags.includes(tag) && tags.length < COURSE_LIMITS.TAGS_MAX_COUNT) {
            setValue('tags', [...tags, tag]);
        }
        setTagInput('');
    }

    function removeTag(tag: string) {
        setValue(
            'tags',
            tags.filter((t) => t !== tag),
        );
    }

    return (
        // FormProvider so the char counters inside the fields can read the live field values.
        <FormProvider {...form}>
            <form id="course-info-form" onSubmit={handleSubmit(onSubmit)} className="space-y-5">
                {/* Title */}
                <FormInput
                    id="title"
                    variant="card"
                    label={t('fieldTitle')}
                    placeholder={t('fieldTitlePlaceholder')}
                    error={errors.title?.message}
                    maxLength={COURSE_LIMITS.TITLE_MAX}
                    showCharLimit
                    {...register('title')}
                />

                <FormTextarea
                    id="description"
                    variant="card"
                    label={t('fieldDescription')}
                    placeholder={t('fieldDescriptionPlaceholder')}
                    error={errors.description?.message}
                    rows={4}
                    maxLength={COURSE_LIMITS.DESCRIPTION_MAX}
                    showCharLimit
                    {...register('description')}
                />

                {/* Category + Price */}
                <div className="grid grid-cols-2 gap-4">
                    <Controller
                        name="categoryId"
                        control={control}
                        render={({ field }) => (
                            <FormSelect
                                id="categoryId"
                                variant="card"
                                label={t('common:general.category')}
                                placeholder={t('fieldCategoryPlaceholder')}
                                error={errors.categoryId?.message}
                                value={field.value}
                                onValueChange={field.onChange}
                                options={categories.map((cat) => ({
                                    value: cat.id,
                                    label: cat.name,
                                }))}
                            />
                        )}
                    />
                    <FormInput
                        id="price"
                        type="number"
                        variant="card"
                        min={COURSE_LIMITS.PRICE_MIN}
                        step={0.01}
                        label={t('fieldPrice')}
                        placeholder={t('fieldPricePlaceholder')}
                        error={errors.price?.message}
                        {...register('price', { valueAsNumber: true })}
                    />
                </div>

                {/* Tags */}
                <div>
                    <label className="mb-1 block text-sm font-medium text-foreground">
                        {t('fieldTags')}
                    </label>
                    <div className="flex min-h-[42px] flex-wrap gap-2 rounded-lg border border-field-border bg-field-card px-3 py-2 shadow-sm focus-within:border-field-focus focus-within:ring-2 focus-within:ring-field-focus/20">
                        {tags.map((tag) => (
                            <span
                                key={tag}
                                className="flex items-center gap-1 rounded bg-primary/10 px-2 py-0.5 text-sm text-primary"
                            >
                                {tag}
                                <button
                                    type="button"
                                    onClick={() => removeTag(tag)}
                                    className="leading-none"
                                >
                                    <X size={12} />
                                </button>
                            </span>
                        ))}
                        <input
                            value={tagInput}
                            onChange={(e) => setTagInput(e.target.value)}
                            onKeyDown={addTag}
                            maxLength={COURSE_LIMITS.TAG_MAX_LENGTH}
                            placeholder={tags.length === 0 ? t('fieldTagsPlaceholder') : ''}
                            className="flex-1 bg-transparent text-sm outline-none"
                        />
                    </div>
                    {errors.tags && (
                        <p className="mt-1 text-xs text-destructive">
                            {errors.tags.message as string}
                        </p>
                    )}
                </div>

                {/* Cover image */}
                <Controller
                    control={control}
                    name="coverImageUrl"
                    render={({ field }) => (
                        <CoverImageUploader
                            // Display the course's current cover; write the newly claimed blob path.
                            value={serverCoverUrl}
                            onChange={(path) => field.onChange(path)}
                        />
                    )}
                />

                {/* Submit */}
                <div className="flex justify-end pt-4">
                    <button
                        type="submit"
                        disabled={isPending}
                        className="text-success-foreground rounded-lg bg-success px-6 py-2.5 text-sm font-medium transition-colors hover:bg-success/90 disabled:cursor-not-allowed disabled:opacity-60"
                    >
                        {isPending ? t('editorUnsaved') : t('common:actions.save')}
                    </button>
                </div>
            </form>
        </FormProvider>
    );
}
