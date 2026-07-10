import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import { APP_ROUTES } from '@/routes/paths';
import { useAuthStore } from '@/store/auth.store';
import { cn } from '@/utils/cn';

export function FinalCTASection() {
    const { t } = useTranslation('landing');
    const isAuthenticated = useAuthStore((s) => !!s.accessToken);

    return (
        <section className="py-20">
            <div className="mx-auto max-w-5xl px-6">
                <div className="relative overflow-hidden rounded-3xl bg-panel p-8 text-center text-panel-foreground md:p-16">
                    <div className="absolute -right-20 -top-20 size-64 rounded-full bg-brand/20 blur-3xl" />
                    <div className="absolute -bottom-20 -left-20 size-64 rounded-full bg-accent/20 blur-3xl" />

                    <div className="relative">
                        <h2 className="font-heading text-3xl font-bold sm:text-4xl md:text-5xl">
                            {t('finalCta.heading.line1')}
                            <br />
                            {t('finalCta.heading.line2')}
                        </h2>
                        <p className="mx-auto mt-5 max-w-xl text-lg text-panel-foreground/70">
                            {t('finalCta.subtitle')}
                        </p>
                        <div className="mt-8 flex flex-col flex-wrap justify-center gap-3 sm:flex-row">
                            {!isAuthenticated && (
                                <Link
                                    to={APP_ROUTES.public.register}
                                    className="rounded-lg bg-brand px-8 py-3.5 font-medium text-brand-foreground transition-colors hover:bg-brand/90"
                                >
                                    {t('finalCta.cta.primary')}
                                </Link>
                            )}
                            <Link
                                to={APP_ROUTES.public.courses}
                                className={cn(
                                    'rounded-lg px-8 py-3.5 font-medium transition-colors',
                                    isAuthenticated
                                        ? 'bg-brand text-brand-foreground hover:bg-brand/90'
                                        : 'border border-panel-foreground/30 text-panel-foreground hover:bg-panel-foreground/10',
                                )}
                            >
                                {t('common:actions.browseCourses')}
                            </Link>
                        </div>
                        <p className="mt-6 text-sm text-panel-foreground/50">
                            {t('finalCta.guarantees')}
                        </p>
                    </div>
                </div>
            </div>
        </section>
    );
}
