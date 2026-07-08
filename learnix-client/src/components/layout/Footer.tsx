import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import {
    GitHubIcon,
    LinkedInIcon,
    TwitterIcon,
    YouTubeIcon,
} from '@/components/common/icons/SocialIcons';
import { BrandLogo } from '@/components/common/ui/BrandLogo';
import { EXTERNAL_LINKS } from '@/const/links.constants';
import { APP_ROUTES } from '@/routes/paths';

interface FooterLink {
    labelKey: string;
    to: string;
    external?: boolean;
}

const productLinks: FooterLink[] = [
    { labelKey: 'footer.links.browseCourses', to: APP_ROUTES.public.courses },
    { labelKey: 'footer.links.categories', to: APP_ROUTES.public.courses },
    { labelKey: 'footer.links.aiTutor', to: APP_ROUTES.public.home + '#features', external: true },
    { labelKey: 'footer.links.certificates', to: APP_ROUTES.student.certificates },
    { labelKey: 'footer.links.achievements', to: APP_ROUTES.student.achievements },
];

const teachLinks: FooterLink[] = [
    { labelKey: 'footer.links.becomeInstructor', to: APP_ROUTES.public.becomeInstructor },
    { labelKey: 'footer.links.instructorHandbook', to: APP_ROUTES.public.faq },
    { labelKey: 'footer.links.revenuePayouts', to: APP_ROUTES.public.faq },
    { labelKey: 'footer.links.courseGuidelines', to: APP_ROUTES.public.faq },
];

const companyLinks: FooterLink[] = [
    { labelKey: 'footer.links.about', to: APP_ROUTES.public.faq },
    { labelKey: 'footer.links.blog', to: APP_ROUTES.public.faq },
    { labelKey: 'footer.links.faq', to: APP_ROUTES.public.faq },
    { labelKey: 'footer.links.contact', to: APP_ROUTES.public.faq },
    { labelKey: 'footer.links.status', to: APP_ROUTES.public.faq },
];

const legalLinks = [
    { labelKey: 'footer.legal.privacy', to: APP_ROUTES.public.faq },
    { labelKey: 'footer.legal.terms', to: APP_ROUTES.public.faq },
    { labelKey: 'footer.legal.cookies', to: APP_ROUTES.public.faq },
    { labelKey: 'footer.legal.accessibility', to: APP_ROUTES.public.faq },
];

const socialLinks = [
    { name: 'twitter', Icon: TwitterIcon, href: EXTERNAL_LINKS.twitter },
    { name: 'github', Icon: GitHubIcon, href: EXTERNAL_LINKS.githubRepo },
    { name: 'linkedin', Icon: LinkedInIcon, href: EXTERNAL_LINKS.linkedin },
    { name: 'youtube', Icon: YouTubeIcon, href: EXTERNAL_LINKS.youtube },
];

function renderLink(link: FooterLink, t: (key: string) => string) {
    const className = 'hover:text-primary';
    const label = t(link.labelKey);
    if (link.external) {
        return (
            <a key={link.labelKey} href={link.to} className={className}>
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
                            {socialLinks.map(({ name, Icon, href }) => (
                                <a
                                    key={name}
                                    href={href}
                                    aria-label={name}
                                    target="_blank"
                                    rel="noopener noreferrer"
                                    className="grid size-9 place-items-center rounded-lg border border-border text-muted-foreground hover:bg-secondary hover:text-primary"
                                >
                                    <Icon className="size-4" />
                                </a>
                            ))}
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
                                {t('footer.sections.company')}
                            </h4>
                            <ul className="space-y-3 text-sm text-muted-foreground">
                                {companyLinks.map((l) => (
                                    <li key={l.labelKey}>{renderLink(l, t)}</li>
                                ))}
                            </ul>
                        </div>
                    </div>
                </div>

                <div className="flex flex-col items-start justify-between gap-4 pt-8 text-sm text-muted-foreground md:flex-row md:items-center">
                    <div>{t('footer.copyright')}</div>
                    <div className="flex flex-wrap gap-x-6 gap-y-2">
                        {legalLinks.map((link) => (
                            <Link key={link.labelKey} to={link.to} className="hover:text-primary">
                                {t(link.labelKey)}
                            </Link>
                        ))}
                    </div>
                </div>
            </div>
        </footer>
    );
}
