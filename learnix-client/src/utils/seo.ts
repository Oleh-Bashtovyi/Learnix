import { APP_ROUTES } from '@/routes/paths';
import type { CourseDetailDto } from '@/types/course.types';
import { env } from '@/utils/env';

/**
 * Related ADRs:
 * - ADR-FRONT-INTL-002: Centralized Page Metadata via a Single <Seo /> Component
 */

export const SITE_NAME = 'Learnix';

/** Fallback social preview image. Lives in `public/`, 1200x630 as required by OG/Twitter. */
export const DEFAULT_OG_IMAGE = '/og-image.png';

/** All prices are quoted in USD (see `formatPrice` in `utils/formatPrice.ts`). */
export const PRICE_CURRENCY = 'USD';

/** Turns a site-relative path (or an already absolute URL) into an absolute URL. */
export function absoluteUrl(pathOrUrl: string): string {
    if (/^https?:\/\//i.test(pathOrUrl)) return pathOrUrl;
    return `${env.SITE_URL}${pathOrUrl.startsWith('/') ? pathOrUrl : `/${pathOrUrl}`}`;
}

type JsonLd = Record<string, unknown>;

export function organizationJsonLd(): JsonLd {
    return {
        '@context': 'https://schema.org',
        '@type': 'Organization',
        name: SITE_NAME,
        url: absoluteUrl('/'),
        logo: absoluteUrl('/favicon.svg'),
    };
}

/** Enables the sitelinks search box in Google results. */
export function webSiteJsonLd(): JsonLd {
    return {
        '@context': 'https://schema.org',
        '@type': 'WebSite',
        name: SITE_NAME,
        url: absoluteUrl('/'),
        potentialAction: {
            '@type': 'SearchAction',
            target: {
                '@type': 'EntryPoint',
                urlTemplate: absoluteUrl('/courses?search={search_term_string}'),
            },
            'query-input': 'required name=search_term_string',
        },
    };
}

export function breadcrumbJsonLd(items: { name: string; path: string }[]): JsonLd {
    return {
        '@context': 'https://schema.org',
        '@type': 'BreadcrumbList',
        itemListElement: items.map((item, index) => ({
            '@type': 'ListItem',
            position: index + 1,
            name: item.name,
            item: absoluteUrl(item.path),
        })),
    };
}

export function faqJsonLd(questions: { q: string; a: string }[]): JsonLd {
    return {
        '@context': 'https://schema.org',
        '@type': 'FAQPage',
        mainEntity: questions.map(({ q, a }) => ({
            '@type': 'Question',
            name: q,
            acceptedAnswer: { '@type': 'Answer', text: a },
        })),
    };
}

export function courseJsonLd(course: CourseDetailDto, language: string): JsonLd {
    const url = absoluteUrl(APP_ROUTES.public.courseDetail(course.id));

    const jsonLd: JsonLd = {
        '@context': 'https://schema.org',
        '@type': 'Course',
        name: course.title,
        description: course.description,
        url,
        inLanguage: language,
        image: course.coverImageUrl
            ? absoluteUrl(course.coverImageUrl)
            : absoluteUrl(DEFAULT_OG_IMAGE),
        provider: {
            '@type': 'Organization',
            name: SITE_NAME,
            url: absoluteUrl('/'),
        },
        author: {
            '@type': 'Person',
            name: course.instructorFullName,
            url: absoluteUrl(APP_ROUTES.public.instructorProfile(course.instructorId)),
        },
        offers: {
            '@type': 'Offer',
            price: course.price,
            priceCurrency: PRICE_CURRENCY,
            category: course.isFree ? 'Free' : 'Paid',
            availability: 'https://schema.org/InStock',
            url,
        },
        // Google requires at least one CourseInstance for Course rich results. `courseWorkload` is
        // omitted deliberately — lessons carry no duration, and inventing one would be false data.
        hasCourseInstance: {
            '@type': 'CourseInstance',
            courseMode: 'Online',
            instructor: {
                '@type': 'Person',
                name: course.instructorFullName,
            },
        },
    };

    // An aggregateRating with zero reviews is a structured-data error, so only emit a rated course.
    if (course.reviewsCount > 0) {
        jsonLd.aggregateRating = {
            '@type': 'AggregateRating',
            ratingValue: course.averageRating,
            reviewCount: course.reviewsCount,
            bestRating: 5,
            worstRating: 1,
        };
    }

    return jsonLd;
}
