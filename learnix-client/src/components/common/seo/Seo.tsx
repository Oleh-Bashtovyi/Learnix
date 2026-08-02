import { Helmet } from 'react-helmet-async';
import { useTranslation } from 'react-i18next';
import { useLocation } from 'react-router-dom';
import { DEFAULT_OG_IMAGE, SITE_NAME, absoluteUrl } from '@/utils/seo';

interface SeoProps {
    title: string;
    description?: string;
    /** Site-relative path or absolute URL. Falls back to the default social preview image. */
    image?: string | null;
    type?: 'website' | 'article' | 'profile';
    /**
     * Canonical path for this page. Defaults to the current pathname, which drops query params —
     * filtered/paginated views therefore point back at the clean URL instead of splitting rank.
     */
    canonicalPath?: string;
    noIndex?: boolean;
    jsonLd?: Record<string, unknown> | Record<string, unknown>[];
}

/**
 * Single source of truth for per-page metadata: title, description, canonical, Open Graph,
 * Twitter Card, robots and JSON-LD.
 *
 * Related ADRs:
 * - ADR-FRONT-INTL-002: Centralized Page Metadata via a Single <Seo /> Component
 */
export function Seo({
    title,
    description,
    image,
    type = 'website',
    canonicalPath,
    noIndex = false,
    jsonLd,
}: SeoProps) {
    const { pathname } = useLocation();
    const { i18n } = useTranslation();

    const canonical = absoluteUrl(canonicalPath ?? pathname);
    const ogImage = absoluteUrl(image || DEFAULT_OG_IMAGE);
    const blocks = jsonLd ? (Array.isArray(jsonLd) ? jsonLd : [jsonLd]) : [];

    return (
        <Helmet>
            <title>{title}</title>
            {description && <meta name="description" content={description} />}
            <link rel="canonical" href={canonical} />
            {noIndex && <meta name="robots" content="noindex,nofollow" />}

            <meta property="og:site_name" content={SITE_NAME} />
            <meta property="og:type" content={type} />
            <meta property="og:url" content={canonical} />
            <meta property="og:title" content={title} />
            {description && <meta property="og:description" content={description} />}
            <meta property="og:image" content={ogImage} />
            <meta property="og:locale" content={i18n.language === 'uk' ? 'uk_UA' : 'en_US'} />

            <meta name="twitter:card" content="summary_large_image" />
            <meta name="twitter:title" content={title} />
            {description && <meta name="twitter:description" content={description} />}
            <meta name="twitter:image" content={ogImage} />

            {blocks.map((block, index) => (
                <script key={index} type="application/ld+json">
                    {JSON.stringify(block)}
                </script>
            ))}
        </Helmet>
    );
}
