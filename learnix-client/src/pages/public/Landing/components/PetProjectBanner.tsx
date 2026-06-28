import { useTranslation } from 'react-i18next';
import { FlaskConical } from 'lucide-react';
import { GitHubIcon } from '@/components/common/icons/SocialIcons';

const GITHUB_URL = 'https://github.com/Oleh-Bashtovyi/Learnix';

export function PetProjectBanner() {
    const { t } = useTranslation('landing');

    return (
        <div className="border-b border-warning/30 bg-warning/10">
            <div className="mx-auto flex max-w-7xl items-center gap-3 px-6 py-3 text-sm text-warning">
                <FlaskConical className="size-4 shrink-0" />
                <span>{t('petProjectBanner.text')}</span>
                <a
                    href={GITHUB_URL}
                    target="_blank"
                    rel="noopener noreferrer"
                    className="ml-auto flex shrink-0 items-center gap-1.5 font-medium hover:underline"
                >
                    <GitHubIcon className="size-3.5" />
                    {t('petProjectBanner.githubLabel')}
                </a>
            </div>
        </div>
    );
}
