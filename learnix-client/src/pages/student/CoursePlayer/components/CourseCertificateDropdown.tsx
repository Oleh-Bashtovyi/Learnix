import { useEffect, useRef, useState } from 'react';
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
                className="flex items-center gap-2 rounded-lg px-3 py-1.5 text-sm font-medium text-muted-foreground transition-colors hover:bg-secondary hover:text-foreground"
            >
                <Trophy className={cn('h-4 w-4', isCompleted ? 'text-primary' : '')} />
                <span className="hidden sm:inline-block">
                    {isCompleted ? 'Get certificate' : 'Your progress'}
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
                                {completedLessons} of {totalLessons} complete
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
                            Get certificate
                        </button>
                    )}
                </div>
            )}
        </div>
    );
}
