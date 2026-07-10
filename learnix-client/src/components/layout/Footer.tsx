import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import { LinkedInIcon } from '@/components/common/icons/SocialIcons';
import { BrandLogo } from '@/components/common/ui/BrandLogo';
import { EXTERNAL_LINKS } from '@/const/links.constants';
import { APP_ROUTES } from '@/routes/paths';

interface FooterLink {
    labelKey: string;
    to: string;
    /** Off-site: opens in a new tab. */
    external?: boolean;
}

// `#`-carrying targets rely on <HashScroll> to reach their anchor after the route loads.
const productLinks: FooterLink[] = [
    { labelKey: 'footer.links.browseCourses', to: APP_ROUTES.public.courses },
    { labelKey: 'footer.links.aiTutor', to: APP_ROUTES.public.home + '#features' },
];

const teachLinks: FooterLink[] = [
    { labelKey: 'footer.links.becomeInstructor', to: APP_ROUTES.public.becomeInstructor },
    { labelKey: 'footer.links.instructorHandbook', to: APP_ROUTES.public.faqInstructors },
    { labelKey: 'footer.links.revenuePayouts', to: APP_ROUTES.public.faqPayments },
];

const projectLinks: FooterLink[] = [
    { labelKey: 'footer.links.about', to: APP_ROUTES.public.about },
    { labelKey: 'footer.links.faq', to: APP_ROUTES.public.faq },
    { labelKey: 'footer.links.contact', to: APP_ROUTES.public.faqSupport },
    { labelKey: 'footer.links.sourceCode', to: EXTERNAL_LINKS.githubRepo, external: true },
];

const legalLinks: FooterLink[] = [
    { labelKey: 'footer.legal.privacy', to: APP_ROUTES.public.aboutPrivacy },
];

function renderLink(link: FooterLink, t: (key: string) => string) {
    const className = 'hover:text-primary';
    const label = t(link.labelKey);

    if (link.external) {
        return (
            <a
                key={link.labelKey}
                href={link.to}
                target="_blank"
                rel="noopener noreferrer"
                className={className}
            >
                {label}
            </a>
        );
    }
    return (
        <Link key={link.labelKey} to={link.to} className={className}>
            {label}
        </Link>
    );
}

export function Footer() {
    const { t } = useTranslation('common');

    return (
        <footer className="border-t border-border bg-card pb-8 pt-16">
            <div className="mx-auto max-w-7xl px-6">
                <div className="flex flex-col gap-10 border-b border-border pb-12 md:flex-row md:justify-between">
                    <div className="max-w-xs md:w-2/5">
                        <BrandLogo />
                        <p className="mt-4 max-w-xs text-sm leading-relaxed text-muted-foreground">
                            {t('footer.description')}
                        </p>
                        <div className="mt-6 flex gap-3">
                            <a
                                href={EXTERNAL_LINKS.linkedin}
                                aria-label="LinkedIn"
                                target="_blank"
                                rel="noopener noreferrer"
                                className="grid size-9 place-items-center rounded-lg border border-border text-muted-foreground hover:bg-secondary hover:text-primary"
                            >
                                <LinkedInIcon className="size-4" />
                            </a>
                        </div>
                    </div>

                    <div className="grid grid-cols-2 gap-8 sm:grid-cols-3 md:w-3/5 md:justify-items-end">
                        <div className="md:text-left">
                            <h4 className="mb-4 font-heading text-sm font-semibold">
                                {t('footer.sections.product')}
                            </h4>
                            <ul className="space-y-3 text-sm text-muted-foreground">
                                {productLinks.map((l) => (
                                    <li key={l.labelKey}>{renderLink(l, t)}</li>
                                ))}
                            </ul>
                        </div>

                        <div className="md:text-left">
                            <h4 className="mb-4 font-heading text-sm font-semibold">
                                {t('footer.sections.teach')}
                            </h4>
                            <ul className="space-y-3 text-sm text-muted-foreground">
                                {teachLinks.map((l) => (
                                    <li key={l.labelKey}>{renderLink(l, t)}</li>
                                ))}
                            </ul>
                        </div>

                        <div className="md:text-left">
                            <h4 className="mb-4 font-heading text-sm font-semibold">
                                {t('footer.sections.project')}
                            </h4>
                            <ul className="space-y-3 text-sm text-muted-foreground">
                                {projectLinks.map((l) => (
                                    <li key={l.labelKey}>{renderLink(l, t)}</li>
                                ))}
                            </ul>
                        </div>
                    </div>
                </div>

                <div className="flex flex-col items-start justify-between gap-4 pt-8 text-sm text-muted-foreground md:flex-row md:items-center">
                    <div>{t('footer.copyright')}</div>
                    <div className="flex flex-wrap gap-x-6 gap-y-2">
                        {legalLinks.map((l) => renderLink(l, t))}
                    </div>
                </div>
            </div>
        </footer>
    );
}
