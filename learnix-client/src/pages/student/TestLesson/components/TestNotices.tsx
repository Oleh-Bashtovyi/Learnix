import { useTranslation } from 'react-i18next';
import { AlertCircle, Clock, Info } from 'lucide-react';
import { formatCooldown } from '@/hooks/lesson/useTestCooldown';
import type { TestPageState } from '../TestLessonPage';

interface TestNoticesProps {
    draftRestored: boolean;
    pageState: TestPageState;
    canAttempt: boolean;
    cooldownSeconds: number | null;
}

export function TestNotices({
    draftRestored,
    pageState,
    canAttempt,
    cooldownSeconds,
}: TestNoticesProps) {
    const { t } = useTranslation('testLesson');

    return (
        <>
            {/* Draft restored notice */}
            {draftRestored && pageState === 'testing' && (
                <div className="flex items-center gap-3 rounded-xl border border-primary/30 bg-primary/5 px-5 py-3 text-sm">
                    <Info className="size-4 shrink-0 text-primary" />
                    <span>{t('draftRestored')}</span>
                </div>
            )}

            {/* Cooldown notice with live countdown */}
            {cooldownSeconds !== null && !canAttempt && (
                <div className="flex items-center gap-3 rounded-xl border border-warning/30 bg-warning/10 px-5 py-4 text-sm">
                    <Clock className="size-5 shrink-0 text-warning" />
                    <span>{t('status.cooldownTimer', formatCooldown(cooldownSeconds))}</span>
                </div>
            )}

            {/* No attempts left */}
            {!canAttempt && cooldownSeconds === null && pageState !== 'submitted' && (
                <div className="flex items-center gap-3 rounded-xl border border-destructive/30 bg-destructive/10 px-5 py-4 text-sm text-destructive">
                    <AlertCircle className="size-5 shrink-0" />
                    {t('status.noAttemptsLeft')}
                </div>
            )}
        </>
    );
}
