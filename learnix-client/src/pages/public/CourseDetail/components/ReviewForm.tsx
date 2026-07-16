import { useEffect } from 'react';
import { Controller, FormProvider, useForm, useWatch } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { zodResolver } from '@hookform/resolvers/zod';
import { toast } from 'sonner';
import { FormTextarea } from '@/components/common/form/FormTextarea';
import { RatingStars } from '@/components/common/ui/RatingStars';
import { REVIEW_LIMITS } from '@/const/review.constants';
import {
    useCreateReview,
    useDeleteReview,
    useUpdateReview,
} from '@/hooks/student/useReviewMutations';
import { type ReviewFormValues, reviewSchema } from '@/schemas/review.schema';
import { useAuthStore } from '@/store/auth.store';
import type { MyReviewDto } from '@/types/review.types';
import { isValidationError } from '@/utils/errors';

interface ReviewFormProps {
    courseId: string;
    existing: MyReviewDto | null;
}

export function ReviewForm({ courseId, existing }: ReviewFormProps) {
    const { t } = useTranslation('courseDetail');
    const user = useAuthStore((s) => s.user);
    const createReview = useCreateReview(courseId);
    const updateReview = useUpdateReview(courseId, existing?.id ?? '');
    const deleteReview = useDeleteReview(courseId, existing?.id ?? '');

    const form = useForm<ReviewFormValues>({
        resolver: zodResolver(reviewSchema),
        defaultValues: { rating: existing?.rating ?? 0, comment: existing?.comment ?? '' },
    });

    useEffect(() => {
        if (existing) {
            form.reset({ rating: existing.rating, comment: existing.comment ?? '' });
        }
    }, [existing, form]);

    async function onSubmit(values: ReviewFormValues) {
        if (!user?.emailVerified) {
            toast.error(t('reviews.emailNotVerified', 'Please confirm your email address first.'));
            return;
        }

        // Remove 3+ consecutive newlines to prevent vertical spam
        const sanitizedComment = values.comment?.trim().replace(/\n{3,}/g, '\n\n') || null;
        const payload = { rating: values.rating, comment: sanitizedComment };
        try {
            if (existing) {
                await updateReview.mutateAsync(payload);
            } else {
                await createReview.mutateAsync(payload);
            }
            if (!existing) form.reset({ rating: 0, comment: '' });
        } catch (error) {
            if (isValidationError(error)) {
                const errors = error.response!.data.errors!;
                Object.entries(errors).forEach(([field, messages]) => {
                    form.setError(
                        (field.charAt(0).toLowerCase() + field.slice(1)) as keyof ReviewFormValues,
                        {
                            message: messages.join('. '),
                        },
                    );
                });
            }
        }
    }

    const isPending = createReview.isPending || updateReview.isPending;
    const title = existing ? t('reviews.editReview') : t('reviews.writeReview');

    const commentValue = useWatch({ control: form.control, name: 'comment' }) || '';
    const ratingValue = useWatch({ control: form.control, name: 'rating' });
    const isUnchanged = existing
        ? ratingValue === existing.rating && commentValue === (existing.comment || '')
        : false;

    return (
        // FormProvider so the char counter inside FormTextarea can read the live comment value.
        <FormProvider {...form}>
            <div className="rounded-xl border border-border bg-card p-5 shadow-sm sm:p-6">
                <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
                    {/* The rating is the only required field — the comment is optional — so it gets
                        the card's largest stars and shares the heading's row rather than a row of its
                        own: at xl they already outweigh the title, and a "Your Rating" caption beside
                        five interactive stars only spends a line saying what they are.

                        Its error sits under them, aligned with them. It used to render at the top of
                        the card, so submitting from the bottom with no stars picked flashed a message
                        the student had to scroll back up to find. */}
                    <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
                        <h3 className="font-heading text-lg font-semibold text-foreground">
                            {title}
                        </h3>
                        <Controller
                            control={form.control}
                            name="rating"
                            render={({ field }) => (
                                <RatingStars
                                    value={field.value}
                                    onChange={field.onChange}
                                    size="xl"
                                    className="gap-1"
                                />
                            )}
                        />
                    </div>
                    {form.formState.errors.rating && (
                        <p className="text-xs text-destructive sm:text-right">
                            {form.formState.errors.rating.message}
                        </p>
                    )}

                    <FormTextarea
                        variant="card"
                        rows={4}
                        maxLength={REVIEW_LIMITS.COMMENT_MAX}
                        showCharLimit="nearLimit"
                        onInput={(e) => {
                            const target = e.currentTarget;
                            target.style.height = 'auto';
                            target.style.height = `${Math.min(target.scrollHeight, 200)}px`;
                        }}
                        placeholder={t('reviews.commentPlaceholder')}
                        error={form.formState.errors.comment?.message}
                        {...form.register('comment')}
                    />

                    {/* Submit tracks the right edge of the field it commits, and of the stars above
                        it. Delete is pushed to the far side: it is the one control here that throws
                        work away, and it has no business sitting under the cursor's path to Submit. */}
                    <div className="flex items-center justify-end gap-4">
                        {existing && (
                            <button
                                type="button"
                                disabled={deleteReview.isPending}
                                onClick={() => deleteReview.mutate()}
                                className="mr-auto text-sm text-destructive hover:underline disabled:opacity-50"
                            >
                                {t('reviews.delete')}
                            </button>
                        )}

                        <button
                            type="submit"
                            disabled={isPending || isUnchanged}
                            className="rounded-lg bg-primary px-5 py-2 text-sm font-medium text-primary-foreground transition-opacity hover:opacity-90 disabled:opacity-50"
                        >
                            {isPending
                                ? t('common:actions.submitting')
                                : existing
                                  ? t('reviews.update')
                                  : t('reviews.submit')}
                        </button>
                    </div>
                </form>
            </div>
        </FormProvider>
    );
}
