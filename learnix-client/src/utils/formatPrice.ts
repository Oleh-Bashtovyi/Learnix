import { PRICE_CURRENCY } from '@/utils/seo';

const currencyFormatter = new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency: PRICE_CURRENCY,
});

/** Formats a paid course price. Callers branch on `isFree`/`price === 0` themselves and show a
 *  localized "Free" label instead of calling this — it only ever renders an actual amount. */
export function formatPrice(price: number): string {
    return currencyFormatter.format(price);
}
