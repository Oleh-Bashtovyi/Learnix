import { Link } from 'react-router-dom';
import { LANDING_PAGE } from '@/const/localization/landingPage';

const { INSTRUCTORS_CTA } = LANDING_PAGE;

const enrollmentBars = [30, 50, 40, 65, 55, 75, 60, 85, 70, 90, 80, 95, 88, 100];

export function InstructorsCTASection() {
    return (
        <section
            id="instructors"
            className="bg-gradient-to-br from-accent/10 via-background to-primary/10 py-20"
        >
            <div className="mx-auto max-w-7xl px-6">
                <div className="grid items-center gap-12 rounded-3xl border border-border bg-card p-10 shadow-xl md:grid-cols-2 md:p-16">
                    <div>
                        <span className="text-sm font-semibold text-accent">
                            {INSTRUCTORS_CTA.tag}
                        </span>
                        <h2 className="mt-2 font-heading text-3xl font-bold md:text-4xl">
                            {INSTRUCTORS_CTA.heading.line1}
                            <br />
                            {INSTRUCTORS_CTA.heading.line2}
                        </h2>
                        <p className="mt-5 leading-relaxed text-muted-foreground">
                            {INSTRUCTORS_CTA.subtitle}
                        </p>

                        <div className="mt-8 grid grid-cols-2 gap-4">
                            <div>
                                <p className="font-heading text-2xl font-bold text-foreground">
                                    {INSTRUCTORS_CTA.stats.earnings.value}
                                </p>
                                <p className="mt-1 text-xs text-muted-foreground">
                                    {INSTRUCTORS_CTA.stats.earnings.label}
                                </p>
                            </div>
                            <div>
                                <p className="font-heading text-2xl font-bold text-foreground">
                                    {INSTRUCTORS_CTA.stats.revenue.value}
                                </p>
                                <p className="mt-1 text-xs text-muted-foreground">
                                    {INSTRUCTORS_CTA.stats.revenue.label}
                                </p>
                            </div>
                        </div>

                        <div className="mt-8 flex gap-3">
                            <Link
                                to="/become-instructor"
                                className="rounded-lg bg-foreground px-6 py-3 font-medium text-background hover:opacity-90"
                            >
                                {INSTRUCTORS_CTA.cta.primary}
                            </Link>
                            <a
                                href="#"
                                className="rounded-lg border border-border px-6 py-3 font-medium hover:bg-secondary"
                            >
                                {INSTRUCTORS_CTA.cta.secondary}
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
                                    {INSTRUCTORS_CTA.preview.instructorName}
                                </p>
                                <p className="text-xs text-muted-foreground">
                                    {INSTRUCTORS_CTA.preview.instructorMeta}
                                </p>
                            </div>
                        </div>

                        <div className="mb-5 grid grid-cols-2 gap-3">
                            <div className="rounded-lg bg-card p-3">
                                <p className="text-xs text-muted-foreground">
                                    {INSTRUCTORS_CTA.preview.thisMonth}
                                </p>
                                <p className="font-heading text-xl font-bold">
                                    {INSTRUCTORS_CTA.preview.thisMonthValue}
                                </p>
                                <p className="mt-1 text-xs text-success">
                                    {INSTRUCTORS_CTA.preview.thisMonthGrowth}
                                </p>
                            </div>
                            <div className="rounded-lg bg-card p-3">
                                <p className="text-xs text-muted-foreground">
                                    {INSTRUCTORS_CTA.preview.newStudents}
                                </p>
                                <p className="font-heading text-xl font-bold">
                                    {INSTRUCTORS_CTA.preview.newStudentsValue}
                                </p>
                                <p className="mt-1 text-xs text-success">
                                    {INSTRUCTORS_CTA.preview.newStudentsGrowth}
                                </p>
                            </div>
                        </div>

                        <div className="rounded-lg bg-card p-4">
                            <p className="mb-2 text-xs text-muted-foreground">
                                {INSTRUCTORS_CTA.preview.enrollmentsLabel}
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
