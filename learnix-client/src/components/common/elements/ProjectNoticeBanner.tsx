import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import { FlaskConical } from 'lucide-react';
import { GitHubIcon } from '@/components/common/icons/SocialIcons';
import { EXTERNAL_LINKS } from '@/const/links.constants';
import { APP_ROUTES } from '@/routes/paths';
import { env } from '@/utils/env';

/**
 * The one place that tells a first-time visitor Learnix is a portfolio project, and the
 * only entry point to `/about` outside the footer. Rendered on the surfaces a stranger
 * lands on — the landing page and the FAQ — so keep it a full-bleed strip: the host
 * supplies nothing, it spans the viewport and lays its own content out on `max-w-7xl`.
 */
export function ProjectNoticeBanner() {
    const { t } = useTranslation('common');

    if (!env.SHOW_PROJECT_BANNER) return null;

    const linkClass = 'flex shrink-0 items-center gap-1.5 font-medium hover:underline';

    return (
        <div className="border-b border-warning/30 bg-warning/10">
            <div className="mx-auto flex max-w-7xl flex-col gap-3 px-6 py-3 text-sm text-warning md:flex-row md:items-center">
                <div className="flex min-w-0 items-start gap-3 md:items-center">
                    <FlaskConical className="mt-0.5 size-4 shrink-0 md:mt-0" />
                    <p className="leading-relaxed">
                        <span className="font-semibold">{t('projectNotice.badge')}:</span>{' '}
                        <span className="text-muted-foreground">{t('projectNotice.text')}</span>
                    </p>
                </div>

                <div className="flex shrink-0 items-center gap-5 pl-7 md:ml-auto md:pl-0">
                    <Link to={APP_ROUTES.public.about} className={linkClass}>
                        {t('projectNotice.aboutLabel')}
                    </Link>
                    <a
                        href={EXTERNAL_LINKS.githubRepo}
                        target="_blank"
                        rel="noopener noreferrer"
                        className={linkClass}
                    >
                        <GitHubIcon className="size-3.5" />
                        {t('projectNotice.githubLabel')}
                    </a>
                </div>
            </div>
        </div>
    );
}
