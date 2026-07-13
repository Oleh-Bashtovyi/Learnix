import { useEffect, useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { ChevronDown, Trophy } from 'lucide-react';
import { CourseCertificateButton } from '@/components/common/course/CourseCertificateButton';
import { cn } from '@/utils/cn';

interface CourseCertificateDropdownProps {
    courseId: string;
    completedLessons: number;
    totalLessons: number;
}

export function CourseCertificateDropdown({
    courseId,
    completedLessons,
    totalLessons,
}: CourseCertificateDropdownProps) {
    const { t } = useTranslation('lessonPlayer');
    const [isOpen, setIsOpen] = useState(false);
    const dropdownRef = useRef<HTMLDivElement>(null);

    const isCompleted = completedLessons === totalLessons && totalLessons > 0;

    useEffect(() => {
        function handleClickOutside(event: MouseEvent) {
            if (dropdownRef.current && !dropdownRef.current.contains(event.target as Node)) {
                setIsOpen(false);
            }
        }

        if (isOpen) {
            document.addEventListener('mousedown', handleClickOutside);
        }
        return () => {
            document.removeEventListener('mousedown', handleClickOutside);
        };
    }, [isOpen]);

    return (
        <div className="relative" ref={dropdownRef}>
            <button
                type="button"
                onClick={() => setIsOpen(!isOpen)}
                className={cn(
                    'flex items-center gap-2 rounded-lg px-3 py-1.5 text-sm font-medium transition-colors',
                    isCompleted
                        ? 'text-achievement hover:bg-achievement/10'
                        : 'text-muted-foreground hover:bg-secondary hover:text-foreground',
                )}
            >
                {/* Gold, and the same gold an unlocked achievement gets — a finished course and an earned
                    badge are the same kind of thing, and the palette should not pretend otherwise. The
                    glow is a drop-shadow on the glyph rather than a box-shadow on the button: it is the
                    trophy that has been won, not the control that holds it. */}
                <Trophy
                    className={cn(
                        'size-4 transition-all',
                        isCompleted && 'drop-shadow-[0_0_6px_hsl(var(--achievement)/0.6)]',
                    )}
                />
                <span className="hidden sm:inline-block">
                    {isCompleted
                        ? t('certificateDropdown.getCertificate')
                        : t('certificateDropdown.yourProgress')}
                </span>
                <ChevronDown
                    className={`size-3 transition-transform ${isOpen ? 'rotate-180' : ''}`}
                />
            </button>

            {isOpen && (
                <div className="animate-in fade-in zoom-in-95 absolute right-0 top-full z-50 mt-2 w-64 rounded-xl border border-border bg-card p-4 shadow-lg duration-200">
                    <div className="mb-4">
                        <div className="mb-1.5 flex items-center justify-between">
                            <span className="text-sm font-medium text-foreground">
                                {t('certificateDropdown.lessonsComplete', {
                                    completed: completedLessons,
                                    total: totalLessons,
                                })}
                            </span>
                            {totalLessons > 0 && (
                                <span className="text-xs font-semibold text-muted-foreground">
                                    {Math.round((completedLessons / totalLessons) * 100)}%
                                </span>
                            )}
                        </div>
                        <div className="h-2 w-full overflow-hidden rounded-full bg-secondary">
                            <div
                                className="h-full rounded-full bg-primary transition-all duration-300"
                                style={{
                                    width:
                                        totalLessons > 0
                                            ? `${(completedLessons / totalLessons) * 100}%`
                                            : '0%',
                                }}
                            />
                        </div>
                    </div>
                    {isCompleted ? (
                        <CourseCertificateButton courseId={courseId} variant="primary" />
                    ) : (
                        <button
                            type="button"
                            disabled
                            className="w-full cursor-not-allowed rounded-lg bg-primary/50 px-4 py-2 text-sm font-medium text-primary-foreground opacity-50"
                        >
                            {t('certificateDropdown.getCertificate')}
                        </button>
                    )}
                </div>
            )}
        </div>
    );
}
