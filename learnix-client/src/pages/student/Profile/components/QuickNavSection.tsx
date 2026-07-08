import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import { Award, ChevronRight, GraduationCap } from 'lucide-react';
import { APP_ROUTES } from '@/routes/paths';

interface QuickNavSectionProps {
    isStudent: boolean;
}

export function QuickNavSection({ isStudent }: QuickNavSectionProps) {
    const { t } = useTranslation('profile');

    return (
        <div className="space-y-4">
            <Link
                to={APP_ROUTES.student.certificates}
                className="flex items-center gap-4 rounded-xl border border-border bg-card p-4 transition-colors hover:bg-secondary sm:p-5"
            >
                <div className="flex size-10 shrink-0 items-center justify-center rounded-lg bg-primary/10">
                    <Award className="size-5 text-primary" />
                </div>
                <div className="min-w-0 flex-1">
                    <p className="text-sm font-medium text-foreground">
                        {t('common:navigation.certificates')}
                    </p>
                    <p className="text-xs text-muted-foreground">{t('certificatesNav.desc')}</p>
                </div>
                <ChevronRight className="size-4 shrink-0 text-muted-foreground" />
            </Link>

            {isStudent && (
                <Link
                    to={APP_ROUTES.public.becomeInstructor}
                    className="flex items-center gap-4 rounded-xl border border-border bg-card p-4 transition-colors hover:bg-secondary sm:p-5"
                >
                    <div className="flex size-10 shrink-0 items-center justify-center rounded-lg bg-accent/10">
                        <GraduationCap className="size-5 text-accent" />
                    </div>
                    <div className="min-w-0 flex-1">
                        <p className="text-sm font-medium text-foreground">
                            {t('becomeInstructorNav.title')}
                        </p>
                        <p className="text-xs text-muted-foreground">
                            {t('becomeInstructorNav.desc')}
                        </p>
                    </div>
                    <ChevronRight className="size-4 shrink-0 text-muted-foreground" />
                </Link>
            )}
        </div>
    );
}
