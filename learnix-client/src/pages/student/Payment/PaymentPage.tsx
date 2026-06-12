import { useEffect } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { AlertTriangle, ShieldCheck, CreditCard, ArrowLeft } from 'lucide-react';
import { useTranslation } from 'react-i18next';

import { useCourseDetail } from '@/hooks/useCourseDetail';
import { paymentsApi } from '@/api/payments.api';
import { paymentSchema, type PaymentFormValues } from '@/schemas/payment.schema';
import { PAYMENT_LIMITS } from '@/const/payment.constants';
import { queryKeys } from '@/api/queryKeys';

export default function PaymentPage() {
    const { courseId } = useParams<{ courseId: string }>();
    const navigate = useNavigate();
    const queryClient = useQueryClient();
    const { t } = useTranslation('payment');

    const { data: course, isLoading: courseLoading } = useCourseDetail(courseId!);

    const form = useForm<PaymentFormValues>({
        resolver: zodResolver(paymentSchema),
        defaultValues: {
            cardNumber: '',
            expiry: '',
            cvv: '',
            cardholderName: '',
        },
    });

    const paymentMutation = useMutation({
        mutationFn: (id: string) => paymentsApi.initiatePayment(id),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: queryKeys.enrollments.mine() });
            toast.success(t('successMessage'));
            // Navigate to the course player. If sections exist, go to first lesson.
            if (
                course?.sections &&
                course.sections.length > 0 &&
                course.sections[0].lessons.length > 0
            ) {
                navigate(`/courses/${courseId}/learn/${course.sections[0].lessons[0].id}`);
            } else {
                navigate(`/dashboard`);
            }
        },
    });

    const { onChange: onCardNumberChange, ...cardNumberReg } = form.register('cardNumber');
    const { onChange: onExpiryChange, ...expiryReg } = form.register('expiry');
    const { onChange: onCvvChange, ...cvvReg } = form.register('cvv');

    function handleCardNumber(e: React.ChangeEvent<HTMLInputElement>) {
        const digits = e.target.value
            .replace(/\D/g, '')
            .slice(0, PAYMENT_LIMITS.CARD_NUMBER_LENGTH);
        e.target.value = digits.replace(/(.{4})/g, '$1 ').trim();
        onCardNumberChange(e);
    }

    function handleExpiry(e: React.ChangeEvent<HTMLInputElement>) {
        const digits = e.target.value.replace(/\D/g, '').slice(0, 4);
        e.target.value = digits.length > 2 ? `${digits.slice(0, 2)}/${digits.slice(2)}` : digits;
        onExpiryChange(e);
    }

    function handleCvv(e: React.ChangeEvent<HTMLInputElement>) {
        e.target.value = e.target.value.replace(/\D/g, '').slice(0, PAYMENT_LIMITS.CVV_MAX);
        onCvvChange(e);
    }

    const onSubmit = (values: PaymentFormValues) => {
        if (!courseId) return;
        paymentMutation.mutate(courseId);
    };

    if (courseLoading) {
        return (
            <div className="mx-auto max-w-4xl px-6 py-12">
                <div className="h-96 animate-pulse rounded-xl bg-muted" />
            </div>
        );
    }

    if (!course) {
        return (
            <div className="mx-auto max-w-4xl px-6 py-20 text-center">
                <p className="text-muted-foreground">{t('courseNotFound')}</p>
                <Link to="/courses" className="mt-4 inline-block text-primary hover:underline">
                    {t('backToCatalog')}
                </Link>
            </div>
        );
    }

    return (
        <div className="mx-auto max-w-4xl px-6 py-12">
            <Link
                to={`/courses/${courseId}`}
                className="mb-6 inline-flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground"
            >
                <ArrowLeft className="h-4 w-4" />
                Back to course
            </Link>

            <h1 className="mb-8 font-heading text-3xl font-bold">{t('title')}</h1>

            <div className="grid gap-8 md:grid-cols-[1fr_350px]">
                {/* Left col: Form */}
                <div>
                    <div className="mb-6 rounded-lg border border-warning/50 bg-warning/10 p-4 text-warning">
                        <div className="flex items-start gap-3">
                            <AlertTriangle className="mt-0.5 h-5 w-5 shrink-0" />
                            <div>
                                <h3 className="font-semibold uppercase tracking-wide">
                                    {t('petProjectWarningTitle')}
                                </h3>
                                <p className="mt-1 text-sm leading-relaxed">
                                    {t('petProjectWarningText')}
                                </p>
                            </div>
                        </div>
                    </div>

                    <div className="rounded-xl border border-border bg-card p-6 shadow-sm">
                        <div className="mb-6 flex items-center gap-2 border-b border-border pb-4">
                            <CreditCard className="h-5 w-5 text-primary" />
                            <h2 className="font-heading text-lg font-semibold">
                                {t('paymentMethod')}
                            </h2>
                        </div>

                        <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-5">
                            {/* Card Number */}
                            <div>
                                <label className="mb-1.5 block text-sm font-medium">
                                    {t('cardNumber')}
                                </label>
                                <input
                                    type="text"
                                    inputMode="numeric"
                                    maxLength={PAYMENT_LIMITS.CARD_NUMBER_LENGTH + 3}
                                    placeholder={t('cardNumberPlaceholder')}
                                    {...cardNumberReg}
                                    onChange={handleCardNumber}
                                    className="w-full rounded-md border border-input bg-transparent px-3 py-2 text-sm shadow-sm focus:outline-none focus:ring-1 focus:ring-ring"
                                />
                                {form.formState.errors.cardNumber && (
                                    <p className="mt-1 text-xs text-destructive">
                                        {form.formState.errors.cardNumber.message}
                                    </p>
                                )}
                            </div>

                            <div className="grid grid-cols-2 gap-4">
                                {/* Expiry */}
                                <div>
                                    <label className="mb-1.5 block text-sm font-medium">
                                        {t('expiry')}
                                    </label>
                                    <input
                                        type="text"
                                        inputMode="numeric"
                                        maxLength={PAYMENT_LIMITS.EXPIRY_MAX_LENGTH}
                                        placeholder={t('expiryPlaceholder')}
                                        {...expiryReg}
                                        onChange={handleExpiry}
                                        className="w-full rounded-md border border-input bg-transparent px-3 py-2 text-sm shadow-sm focus:outline-none focus:ring-1 focus:ring-ring"
                                    />
                                    {form.formState.errors.expiry && (
                                        <p className="mt-1 text-xs text-destructive">
                                            {form.formState.errors.expiry.message}
                                        </p>
                                    )}
                                </div>
                                {/* CVV */}
                                <div>
                                    <label className="mb-1.5 block text-sm font-medium">
                                        {t('cvv')}
                                    </label>
                                    <input
                                        type="text"
                                        inputMode="numeric"
                                        maxLength={PAYMENT_LIMITS.CVV_MAX}
                                        placeholder={t('cvvPlaceholder')}
                                        {...cvvReg}
                                        onChange={handleCvv}
                                        className="w-full rounded-md border border-input bg-transparent px-3 py-2 text-sm shadow-sm focus:outline-none focus:ring-1 focus:ring-ring"
                                    />
                                    {form.formState.errors.cvv && (
                                        <p className="mt-1 text-xs text-destructive">
                                            {form.formState.errors.cvv.message}
                                        </p>
                                    )}
                                </div>
                            </div>

                            {/* Name */}
                            <div>
                                <label className="mb-1.5 block text-sm font-medium">
                                    {t('cardholderName')}
                                </label>
                                <input
                                    type="text"
                                    placeholder={t('cardholderNamePlaceholder')}
                                    {...form.register('cardholderName')}
                                    className="w-full rounded-md border border-input bg-transparent px-3 py-2 text-sm shadow-sm focus:outline-none focus:ring-1 focus:ring-ring"
                                />
                                {form.formState.errors.cardholderName && (
                                    <p className="mt-1 text-xs text-destructive">
                                        {form.formState.errors.cardholderName.message}
                                    </p>
                                )}
                            </div>

                            <button
                                type="submit"
                                disabled={paymentMutation.isPending}
                                className="mt-6 flex w-full items-center justify-center gap-2 rounded-lg bg-primary px-4 py-3 font-medium text-primary-foreground transition-opacity hover:opacity-90 disabled:opacity-50"
                            >
                                {paymentMutation.isPending ? (
                                    t('processing')
                                ) : (
                                    <>
                                        <ShieldCheck className="h-5 w-5" />
                                        {t('payButton')}
                                    </>
                                )}
                            </button>
                        </form>
                    </div>
                </div>

                {/* Right col: Order Summary */}
                <aside className="shrink-0">
                    <div className="sticky top-6 rounded-xl border border-border bg-card p-6 shadow-sm">
                        <h2 className="font-heading text-lg font-semibold">{t('courseDetails')}</h2>

                        {course.coverImageUrl && (
                            <img
                                src={course.coverImageUrl}
                                alt={course.title}
                                className="mb-4 mt-4 aspect-video w-full rounded-lg object-cover"
                            />
                        )}

                        <h3 className="mt-4 line-clamp-2 font-medium">{course.title}</h3>

                        <div className="mt-6 flex items-center justify-between border-t border-border pt-4">
                            <span className="text-muted-foreground">{t('priceLabel')}</span>
                            <span className="font-heading text-2xl font-bold">
                                {course.price === 0 ? t('free') : `$${course.price}`}
                            </span>
                        </div>
                    </div>
                </aside>
            </div>
        </div>
    );
}
