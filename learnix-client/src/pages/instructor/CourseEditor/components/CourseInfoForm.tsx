import { useState } from 'react';
import type { KeyboardEvent } from 'react';
import { Controller, useForm, useWatch } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { zodResolver } from '@hookform/resolvers/zod';
import { X } from 'lucide-react';
import { FormInput } from '@/components/common/form/FormInput';
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

    const {
        register,
        handleSubmit,
        control,
        setValue,
        formState: { errors },
    } = useForm<CourseInfoFormData>({
        resolver: zodResolver(courseInfoSchema),
        defaultValues: {
            title: course?.title ?? '',
            description: course?.description ?? '',
            categoryId: course?.categoryId ?? '',
            price: course?.price ?? 0,
            coverImageUrl: course?.coverImageUrl ?? null,
            tags: course?.tags ?? [],
        },
    });

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
        <form id="course-info-form" onSubmit={handleSubmit(onSubmit)} className="space-y-5">
            {/* Title */}
            <FormInput
                id="title"
                label={t('fieldTitle')}
                placeholder={t('fieldTitlePlaceholder')}
                error={errors.title?.message}
                {...register('title')}
            />

            {/* Description */}
            <div>
                <label className="mb-1 block text-sm font-medium text-foreground">
                    {t('fieldDescription')}
                </label>
                <textarea
                    {...register('description')}
                    rows={4}
                    placeholder={t('fieldDescriptionPlaceholder')}
                    className="w-full resize-none rounded-lg border border-input bg-background px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-ring"
                />
                {errors.description && (
                    <p className="mt-1 text-xs text-destructive">{errors.description.message}</p>
                )}
            </div>

            {/* Category + Price */}
            <div className="grid grid-cols-2 gap-4">
                <div>
                    <label className="mb-1 block text-sm font-medium text-foreground">
                        {t('fieldCategory')}
                    </label>
                    <select
                        {...register('categoryId')}
                        className="w-full rounded-lg border border-input bg-background px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-ring"
                    >
                        <option value="">{t('fieldCategoryPlaceholder')}</option>
                        {categories.map((cat) => (
                            <option key={cat.id} value={cat.id}>
                                {cat.name}
                            </option>
                        ))}
                    </select>
                    {errors.categoryId && (
                        <p className="mt-1 text-xs text-destructive">{errors.categoryId.message}</p>
                    )}
                </div>
                <FormInput
                    id="price"
                    type="number"
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
                <div className="flex min-h-[42px] flex-wrap gap-2 rounded-lg border border-input bg-background px-3 py-2">
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
                        placeholder={tags.length === 0 ? t('fieldTagsPlaceholder') : ''}
                        className="flex-1 bg-transparent text-sm outline-none"
                    />
                </div>
                {errors.tags && (
                    <p className="mt-1 text-xs text-destructive">{errors.tags.message as string}</p>
                )}
            </div>

            {/* Cover image */}
            <Controller
                control={control}
                name="coverImageUrl"
                render={({ field }) => (
                    <CoverImageUploader
                        value={field.value ?? null}
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
                    {isPending ? t('editorUnsaved') : t('btnSave')}
                </button>
            </div>
        </form>
    );
}
