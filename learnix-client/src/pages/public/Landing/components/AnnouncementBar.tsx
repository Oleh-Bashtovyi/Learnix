import { useTranslation } from 'react-i18next';

export function AnnouncementBar() {
    const { t } = useTranslation('landing');

    return (
        <div className="bg-panel px-4 py-2.5 text-center text-sm text-panel-foreground">
            {t('announcement.text')}
            <a href={t('announcement.linkHref')} className="ml-1 underline hover:text-primary">
                {t('announcement.linkLabel')}
            </a>
        </div>
    );
}
