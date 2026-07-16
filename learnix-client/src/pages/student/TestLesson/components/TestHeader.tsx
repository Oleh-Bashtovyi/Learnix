import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import { ChevronLeft } from 'lucide-react';
import { ThemeSwitcher } from '@/components/common/ui/ThemeSwitcher';
import { APP_ROUTES } from '@/routes/paths';
import { cn } from '@/utils/cn';

/** Progress and the submit action for a live attempt. Absent once there is nothing left to sit. */
interface AttemptControls {
    answeredCount: number;
    totalQuestions: number;
    /** False until the attempt has actually been opened on the server. */
    isReady: boolean;
    isSubmitPending: boolean;
    onSubmit: () => void;
}

interface TestHeaderProps {
    courseId: string;
    lessonId: string;
    attempt?: AttemptControls;
}

export function TestHeader({ courseId, lessonId, attempt }: TestHeaderProps) {
    const { t } = useTranslation('testLesson');

    // Neither the progress nor the submit belongs to any one question, and both used to scroll away
    // with the top of the form — leaving a student who had answered everything to hunt down the page
    // for the button. The header is already sticky, so they live here and stay reachable.
    return (
        <header className="sticky top-0 z-10 border-b border-border bg-card">
            <div className="mx-auto flex h-14 max-w-4xl items-center justify-between gap-4 px-4 sm:px-6">
                <Link
                    to={APP_ROUTES.student.learnLesson(courseId, lessonId)}
                    className="inline-flex shrink-0 items-center gap-1.5 text-sm text-muted-foreground transition-colors hover:text-foreground"
                >
                    <ChevronLeft className="size-4" />
                    {t('header.backToLesson')}
                </Link>

                {/* The theme toggle sits ahead of the pair so progress stays next to the button it
                    is about, and outside the attempt block so it survives the submitted state. */}
                <div className="flex items-center gap-3">
                    <ThemeSwitcher />

                    {/* The bar is the first thing to go on a narrow header: the submit button has
                        to fit, and the count says the same thing in words. */}
                    {attempt && attempt.totalQuestions > 0 && (
                        <div className="hidden items-center gap-2 text-xs text-muted-foreground sm:flex">
                            <span className="whitespace-nowrap">
                                {t('form.answeredOf', {
                                    answered: attempt.answeredCount,
                                    total: attempt.totalQuestions,
                                })}
                            </span>
                            <div className="h-1.5 w-24 overflow-hidden rounded-full bg-secondary">
                                <div
                                    className="h-full rounded-full bg-primary transition-all"
                                    style={{
                                        width: `${(attempt.answeredCount / attempt.totalQuestions) * 100}%`,
                                    }}
                                />
                            </div>
                        </div>
                    )}

                    {attempt && (
                        <button
                            type="button"
                            onClick={attempt.onSubmit}
                            disabled={attempt.isSubmitPending || !attempt.isReady}
                            className={cn(
                                'shrink-0 rounded-lg px-5 py-2 text-sm font-medium text-primary-foreground transition-colors',
                                attempt.isSubmitPending || !attempt.isReady
                                    ? 'cursor-not-allowed bg-primary/60'
                                    : 'bg-primary hover:bg-primary/90',
                            )}
                        >
                            {attempt.isSubmitPending
                                ? t('common:actions.submitting')
                                : !attempt.isReady
                                  ? t('form.starting')
                                  : t('form.submitButton')}
                        </button>
                    )}
                </div>
            </div>
        </header>
    );
}
