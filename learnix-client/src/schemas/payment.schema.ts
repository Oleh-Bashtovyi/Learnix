import { z } from 'zod';
import { PAYMENT_LIMITS } from '@/const/payment.constants';

export const paymentSchema = z.object({
    cardNumber: z
        .string()
        .transform((val) => val.replace(/\s/g, ''))
        .pipe(
            z
                .string()
                .min(
                    PAYMENT_LIMITS.CARD_NUMBER_LENGTH,
                    `Card number must be ${PAYMENT_LIMITS.CARD_NUMBER_LENGTH} digits`,
                )
                .max(
                    PAYMENT_LIMITS.CARD_NUMBER_LENGTH,
                    `Card number must be ${PAYMENT_LIMITS.CARD_NUMBER_LENGTH} digits`,
                )
                .regex(/^\d+$/, 'Card number can only contain digits'),
        ),
    expiry: z.string().regex(/^(0[1-9]|1[0-2])\/\d{2}$/, 'Expiry must be in MM/YY format'),
    cvv: z
        .string()
        .min(PAYMENT_LIMITS.CVV_MIN, `CVV must be ${PAYMENT_LIMITS.CVV_MIN} digits`)
        .max(PAYMENT_LIMITS.CVV_MAX, `CVV must be at most ${PAYMENT_LIMITS.CVV_MAX} digits`)
        .regex(/^\d+$/, 'CVV can only contain digits'),
    cardholderName: z
        .string()
        .min(
            PAYMENT_LIMITS.CARDHOLDER_NAME_MIN,
            `Name must be at least ${PAYMENT_LIMITS.CARDHOLDER_NAME_MIN} characters`,
        ),
});

export type PaymentFormValues = z.infer<typeof paymentSchema>;
