import { Download, Clock, Award } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { useMyCertificates } from '@/hooks/useMyCertificates';
import { cn } from '@/utils/cn';

interface CourseCertificateButtonProps {
    courseId: string;
    variant?: 'primary' | 'secondary' | 'outline' | 'ghost';
    className?: string;
    showIconOnlyOnMobile?: boolean;
}

export function CourseCertificateButton({ 
    courseId, 
    variant = 'primary', 
    className, 
    showIconOnlyOnMobile = false 
}: CourseCertificateButtonProps) {
    const { t } = useTranslation('certificates');
    const { data: certificates, isLoading } = useMyCertificates();

    const certificate = certificates?.find(c => c.courseId === courseId);

    if (isLoading) {
        return (
            <div className={cn("inline-flex items-center justify-center rounded-lg bg-muted px-4 py-2 animate-pulse", className)}>
                <div className="h-4 w-24 bg-muted-foreground/20 rounded" />
            </div>
        );
    }

    if (!certificate) {
        return null;
    }

    const baseStyles = "inline-flex items-center justify-center gap-2 rounded-lg text-sm font-medium transition-colors disabled:opacity-50 disabled:pointer-events-none";
    
    const variants = {
        primary: "bg-primary text-primary-foreground hover:bg-primary/90",
        secondary: "bg-secondary text-secondary-foreground hover:bg-secondary/80",
        outline: "border border-border bg-transparent hover:bg-secondary",
        ghost: "hover:bg-accent hover:text-accent-foreground text-muted-foreground",
    };

    if (!certificate.isReady || !certificate.downloadUrl) {
        return (
            <span className={cn(baseStyles, "bg-muted text-muted-foreground px-4 py-2 cursor-not-allowed", className)}>
                <Clock className="h-4 w-4 shrink-0" />
                <span className={cn("whitespace-nowrap", showIconOnlyOnMobile && "hidden sm:inline")}>
                    {t('status.generating', { defaultValue: 'Generating...' })}
                </span>
            </span>
        );
    }

    return (
        <a
            href={certificate.downloadUrl}
            target="_blank"
            rel="noopener noreferrer"
            className={cn(baseStyles, variants[variant], "px-4 py-2", className)}
        >
            <Award className="h-4 w-4 shrink-0" />
            <span className={cn("whitespace-nowrap", showIconOnlyOnMobile && "hidden sm:inline")}>
                {t('actions.download', { defaultValue: 'Download Certificate' })}
            </span>
        </a>
    );
}
