import { useEffect, useRef, useState } from 'react';
import type { KeyboardEvent } from 'react';
import { Controller, FormProvider, useForm, useWatch } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { zodResolver } from '@hookform/resolvers/zod';
import { X } from 'lucide-react';
import { FormInput } from '@/components/common/form/FormInput';
import { FormSelect } from '@/components/common/form/FormSelect';
import { FormTextarea } from '@/components/common/form/FormTextarea';
import { COURSE_LIMITS, COURSE_PRICE_PRESETS } from '@/const/course.constants';
import { useCategories } from '@/hooks/course/useCategories';
import { usePopularTags } from '@/hooks/course/usePopularTags';
import { type CourseInfoFormData, courseInfoSchema } from '@/schemas/course.schema';
import type { CourseForEditDto } from '@/types/course.types';
import { cn } from '@/utils/cn';
import { CoverImageUploader } from './CoverImageUploader';

/** The server offers up to 20; this is how many of them fit under the field as suggestions. */
const TAG_SUGGESTIONS_SHOWN = 10;

interface Props {
    course?: CourseForEditDto;
    onSubmit: (data: CourseInfoFormData) => void;
    /** Reports whether the form holds edits the course does not have yet. */
    onDirtyChange?: (isDirty: boolean) => void;
}

export function CourseInfoForm({ course, onSubmit, onDirtyChange }: Props) {
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
        formState: { errors, isDirty },
    } = form;

    useEffect(() => {
        onDirtyChange?.(isDirty);
    }, [isDirty, onDirtyChange]);

    // A saved course comes back with a new UpdatedAt, and that is the only refetch the form may take
    // over its own state: re-seeding on every refetch — a window refocus, a sibling query — would
    // overwrite whatever is being typed at that moment. Re-seeding here is what clears isDirty once
    // the edits have actually reached the server.
    const syncedUpdatedAt = useRef(course?.updatedAt);
    useEffect(() => {
        if (!course || course.updatedAt === syncedUpdatedAt.current) return;
        syncedUpdatedAt.current = course.updatedAt;
        form.reset({
            title: course.title,
            description: course.description,
            categoryId: course.categoryId,
            price: course.price,
            coverImageUrl: null,
            tags: course.tags,
        });
    }, [course, form]);

    // A blob path is a one-shot claim: committing it moves the blob out of temp-uploads and deletes
    // the original. Once the save lands, the server's cover URL changes — that is the signal that the
    // claim was spent, so drop it. Re-submitting it would fail with "file not found or expired".
    const serverCoverUrl = course?.coverImageUrl ?? null;
    useEffect(() => {
        setValue('coverImageUrl', null);
    }, [serverCoverUrl, setValue]);

    const tags = useWatch({ control, name: 'tags' }) ?? [];
    const price = useWatch({ control, name: 'price' });
    const categoryId = useWatch({ control, name: 'categoryId' });
    const [tagInput, setTagInput] = useState('');

    // Suggestions follow the category currently in the form, and exclude tags the course already
    // carries: every chip shown is one that adds something when clicked.
    const { data: popularTags = [] } = usePopularTags(categoryId || undefined);
    const suggestedTags =
        tags.length >= COURSE_LIMITS.TAGS_MAX_COUNT
            ? []
            : popularTags.filter((tag) => !tags.includes(tag)).slice(0, TAG_SUGGESTIONS_SHOWN);

    function addTagValue(value: string) {
        const tag = value.trim().toLowerCase();
        if (tag && !tags.includes(tag) && tags.length < COURSE_LIMITS.TAGS_MAX_COUNT) {
            setValue('tags', [...tags, tag], { shouldDirty: true, shouldValidate: true });
        }
    }

    function addTag(e: KeyboardEvent<HTMLInputElement>) {
        if (e.key !== 'Enter' && e.key !== ',') return;
        e.preventDefault();
        addTagValue(tagInput);
        setTagInput('');
    }

    function removeTag(tag: string) {
        setValue(
            'tags',
            tags.filter((t) => t !== tag),
            { shouldDirty: true, shouldValidate: true },
        );
    }

    return (
        // FormProvider so the char counters inside the fields can read the live field values.
        <FormProvider {...form}>
            {/* Title above, then the description as the working canvas on the left and the short
                fields stacked on the right. The description is the only field with no natural
                height, so it takes whatever height the panel has left. */}
            <form
                id="course-info-form"
                onSubmit={handleSubmit(onSubmit)}
                className="grid gap-x-6 gap-y-5 2xl:h-full 2xl:grid-cols-[minmax(0,3fr)_minmax(0,2fr)] 2xl:grid-rows-[auto_minmax(0,1fr)]"
            >
                <div className="2xl:col-span-2">
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
                </div>

                <FormTextarea
                    id="description"
                    variant="card"
                    label={t('fieldDescription')}
                    placeholder={t('fieldDescriptionPlaceholder')}
                    error={errors.description?.message}
                    rows={4}
                    maxLength={COURSE_LIMITS.DESCRIPTION_MAX}
                    showCharLimit
                    containerClassName="flex min-w-0 flex-col"
                    className="min-h-28 flex-1"
                    {...register('description')}
                />

                <div className="space-y-5">
                    {/* A cover can only be claimed by an existing course: CreateCourseCommand has no
                        field for one, so until the course is saved this says so rather than
                        offering an upload the save would drop. */}
                    {/* Capped while it is stacked under the fields, so a one-column form does not
                        end in a picture as wide as the card. */}
                    {course ? (
                        <div className="max-w-md 2xl:max-w-none">
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
                        </div>
                    ) : (
                        <div className="max-w-md space-y-1.5 2xl:max-w-none">
                            <span className="block text-sm font-medium text-foreground">
                                {t('coverImageLabel')}
                            </span>
                            <p className="flex aspect-video items-center justify-center rounded-lg border-2 border-dashed border-border p-6 text-center text-sm text-muted-foreground">
                                {t('coverImageAfterSave')}
                            </p>
                        </div>
                    )}

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
                        <div>
                            <FormInput
                                id="price"
                                type="number"
                                variant="card"
                                min={COURSE_LIMITS.PRICE_MIN}
                                step={0.01}
                                label={t('fieldPrice')}
                                placeholder={t('fieldPricePlaceholder')}
                                error={errors.price?.message}
                                // No spinner arrows: at a step of one cent they are no way to reach
                                // a price, and they sit over the value while the field has focus.
                                className="[appearance:textfield] [&::-webkit-inner-spin-button]:appearance-none [&::-webkit-outer-spin-button]:appearance-none"
                                {...register('price', { valueAsNumber: true })}
                            />
                            {/* The prices a course actually gets sold at, one click away. */}
                            <div className="mt-2 flex flex-wrap gap-1.5">
                                {COURSE_PRICE_PRESETS.map((preset) => (
                                    <button
                                        key={preset}
                                        type="button"
                                        onClick={() =>
                                            setValue('price', preset, {
                                                shouldDirty: true,
                                                shouldValidate: true,
                                            })
                                        }
                                        className={cn(
                                            'rounded-md border px-2 py-0.5 text-xs transition-colors',
                                            price === preset
                                                ? 'border-primary bg-primary/10 text-primary'
                                                : 'border-border text-muted-foreground hover:border-primary hover:text-primary',
                                        )}
                                    >
                                        {preset === 0 ? t('common:general.free') : `$${preset}`}
                                    </button>
                                ))}
                            </div>
                        </div>
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
                        {/* Suggestions, not defaults: the tags published courses in this category
                            already carry, so a new course can join the vocabulary students search
                            by. Nothing is applied until one is clicked. */}
                        {suggestedTags.length > 0 && (
                            <div className="mt-2">
                                <p className="mb-1.5 text-xs text-muted-foreground">
                                    {t('fieldTagsSuggested')}
                                </p>
                                <div className="flex flex-wrap gap-1.5">
                                    {suggestedTags.map((tag) => (
                                        <button
                                            key={tag}
                                            type="button"
                                            onClick={() => addTagValue(tag)}
                                            className="rounded-md border border-border px-2 py-0.5 text-xs text-muted-foreground transition-colors hover:border-primary hover:text-primary"
                                        >
                                            + {tag}
                                        </button>
                                    ))}
                                </div>
                            </div>
                        )}
                    </div>
                </div>
            </form>
        </FormProvider>
    );
}
