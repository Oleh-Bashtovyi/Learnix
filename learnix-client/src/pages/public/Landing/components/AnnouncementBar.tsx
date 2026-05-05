import { LANDING_PAGE } from '@/const/localization/landingPage';

const { ANNOUNCEMENT } = LANDING_PAGE;

export function AnnouncementBar() {
    return (
        <div className="bg-foreground px-4 py-2.5 text-center text-sm text-background">
            {ANNOUNCEMENT.text}
            <a href={ANNOUNCEMENT.linkHref} className="ml-1 underline hover:text-primary">
                {ANNOUNCEMENT.linkLabel}
            </a>
        </div>
    );
}
