import { z } from 'zod';
import { PAYMENT_LIMITS } from '@/const/payment.constants';

/**
 * Related ADRs:
 * - ADR-FRONT-FORMS-002: Zod Schemas as Source of Truth
 */
export const paymentSchema = z.object({
    cardNumber: z
        .string()
        .transform((val) => val.replace(/\s/g, ''))
        .pipe(
            z
                .string()
                .min(PAYMENT_LIMITS.CARD_NUMBER_LENGTH)
                .max(PAYMENT_LIMITS.CARD_NUMBER_LENGTH)
                // Matches strings containing only digits (0-9)
                .refine((val) => /^\d+$/.test(val), {
                    params: { i18n: 'custom.card_digits_only' },
                }),
        ),
    expiry: z
        .string()
        // Matches MM/YY format where month is 01-12
        .refine((val) => /^(0[1-9]|1[0-2])\/\d{2}$/.test(val), {
            params: { i18n: 'custom.expiry_format' },
        }),
    cvv: z
        .string()
        .min(PAYMENT_LIMITS.CVV_MIN)
        .max(PAYMENT_LIMITS.CVV_MAX)
        // Matches strings containing only digits (0-9)
        .refine((val) => /^\d+$/.test(val), { params: { i18n: 'custom.cvv_digits_only' } }),
    cardholderName: z.string().min(PAYMENT_LIMITS.CARDHOLDER_NAME_MIN),
});

export type PaymentFormValues = z.infer<typeof paymentSchema>;
