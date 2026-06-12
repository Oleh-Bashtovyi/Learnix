import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';

const enrollmentBars = [30, 50, 40, 65, 55, 75, 60, 85, 70, 90, 80, 95, 88, 100];

export function InstructorsCTASection() {
    const { t } = useTranslation('landing');

    return (
        <section
            id="instructors"
            className="bg-gradient-to-br from-accent/10 via-background to-primary/10 py-20"
        >
            <div className="mx-auto max-w-7xl px-6">
                <div className="grid items-center gap-12 rounded-3xl border border-border bg-card p-6 shadow-xl md:grid-cols-2 md:p-16">
                    <div>
                        <span className="text-sm font-semibold text-accent">
                            {t('instructorsCta.tag')}
                        </span>
                        <h2 className="mt-2 font-heading text-3xl font-bold md:text-4xl">
                            {t('instructorsCta.heading.line1')}
                            <br />
                            {t('instructorsCta.heading.line2')}
                        </h2>
                        <p className="mt-5 leading-relaxed text-muted-foreground">
                            {t('instructorsCta.subtitle')}
                        </p>

                        <div className="mt-8 grid grid-cols-2 gap-4">
                            <div>
                                <p className="font-heading text-2xl font-bold text-foreground">
                                    {t('instructorsCta.stats.earnings.value')}
                                </p>
                                <p className="mt-1 text-xs text-muted-foreground">
                                    {t('instructorsCta.stats.earnings.label')}
                                </p>
                            </div>
                            <div>
                                <p className="font-heading text-2xl font-bold text-foreground">
                                    {t('instructorsCta.stats.revenue.value')}
                                </p>
                                <p className="mt-1 text-xs text-muted-foreground">
                                    {t('instructorsCta.stats.revenue.label')}
                                </p>
                            </div>
                        </div>

                        <div className="mt-8 flex flex-col gap-3 sm:flex-row">
                            <Link
                                to="/become-instructor"
                                className="rounded-lg bg-foreground px-6 py-3 text-center font-medium text-background hover:opacity-90"
                            >
                                {t('instructorsCta.cta.primary')}
                            </Link>
                            <a
                                href="#"
                                className="rounded-lg border border-border px-6 py-3 text-center font-medium hover:bg-secondary"
                            >
                                {t('instructorsCta.cta.secondary')}
                            </a>
                        </div>
                    </div>

                    {/* Instructor dashboard preview */}
                    <div className="rounded-2xl border border-border bg-secondary/50 p-6">
                        <div className="mb-5 flex items-center gap-3">
                            <div className="grid h-10 w-10 place-items-center rounded-full bg-accent font-medium text-accent-foreground">
                                JD
                            </div>
                            <div>
                                <p className="text-sm font-medium">
                                    {t('instructorsCta.preview.instructorName')}
                                </p>
                                <p className="text-xs text-muted-foreground">
                                    {t('instructorsCta.preview.instructorMeta')}
                                </p>
                            </div>
                        </div>

                        <div className="mb-5 grid grid-cols-2 gap-3">
                            <div className="rounded-lg bg-card p-3">
                                <p className="text-xs text-muted-foreground">
                                    {t('instructorsCta.preview.thisMonth')}
                                </p>
                                <p className="font-heading text-xl font-bold">
                                    {t('instructorsCta.preview.thisMonthValue')}
                                </p>
                                <p className="mt-1 text-xs text-success">
                                    {t('instructorsCta.preview.thisMonthGrowth')}
                                </p>
                            </div>
                            <div className="rounded-lg bg-card p-3">
                                <p className="text-xs text-muted-foreground">
                                    {t('instructorsCta.preview.newStudents')}
                                </p>
                                <p className="font-heading text-xl font-bold">
                                    {t('instructorsCta.preview.newStudentsValue')}
                                </p>
                                <p className="mt-1 text-xs text-success">
                                    {t('instructorsCta.preview.newStudentsGrowth')}
                                </p>
                            </div>
                        </div>

                        <div className="rounded-lg bg-card p-4">
                            <p className="mb-2 text-xs text-muted-foreground">
                                {t('instructorsCta.preview.enrollmentsLabel')}
                            </p>
                            <div className="flex h-16 items-end gap-1">
                                {enrollmentBars.map((h, i) => (
                                    <div
                                        key={i}
                                        className={`flex-1 rounded-t ${i < 7 ? 'bg-primary/30' : 'bg-primary'}`}
                                        style={{ height: `${h}%` }}
                                    />
                                ))}
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </section>
    );
}
