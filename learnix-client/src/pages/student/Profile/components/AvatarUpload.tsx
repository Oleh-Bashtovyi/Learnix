import React, { useRef } from 'react';
import { useTranslation } from 'react-i18next';
import { CalendarDays, Camera, User } from 'lucide-react';

interface AvatarUploadProps {
    firstName?: string;
    lastName?: string;
    displayAvatar: string | null;
    isUploading: boolean;
    createdAt?: string;
    onAvatarChange: (e: React.ChangeEvent<HTMLInputElement>) => void;
}

export function AvatarUpload({
    firstName,
    lastName,
    displayAvatar,
    isUploading,
    createdAt,
    onAvatarChange,
}: AvatarUploadProps) {
    const { t, i18n } = useTranslation('profile');
    const fileInputRef = useRef<HTMLInputElement>(null);

    return (
        <div className="flex shrink-0 flex-col md:w-48">
            <div className="flex flex-row items-center gap-5 sm:gap-6 md:flex-col md:items-start md:gap-0 md:text-left">
                <div className="relative shrink-0">
                    <div className="flex size-20 items-center justify-center overflow-hidden rounded-full border-4 border-background bg-muted shadow-sm sm:size-24 md:size-32">
                        {displayAvatar !== null ? (
                            <img
                                src={displayAvatar}
                                alt="Avatar"
                                className="size-full object-cover"
                            />
                        ) : (
                            <User className="size-10 text-muted-foreground md:size-14" />
                        )}
                    </div>
                    <button
                        type="button"
                        onClick={() => fileInputRef.current?.click()}
                        disabled={isUploading}
                        title={isUploading ? t('avatar.uploading') : t('avatar.upload')}
                        className="absolute bottom-0 right-0 flex size-8 items-center justify-center rounded-full border-2 border-background bg-accent text-accent-foreground shadow-md transition-transform hover:scale-105 hover:opacity-90 disabled:scale-100 disabled:opacity-50 md:bottom-1 md:right-1 md:size-9"
                    >
                        <Camera className="size-3.5 md:size-4" />
                    </button>
                    <input
                        ref={fileInputRef}
                        type="file"
                        accept="image/jpeg,image/png,image/webp"
                        className="hidden"
                        onChange={onAvatarChange}
                    />
                </div>
                <div className="flex flex-col text-left md:mt-4">
                    <h3 className="font-heading text-lg font-semibold text-foreground">
                        {firstName} {lastName}
                    </h3>
                    <p className="mt-1 text-xs text-muted-foreground md:max-w-[200px]">
                        {t('avatar.hint')}
                    </p>
                </div>
            </div>

            <div className="mt-6 flex w-full flex-col gap-4 border-t border-border pt-5 text-sm text-muted-foreground md:mt-8 md:flex-col md:items-start md:justify-start md:pt-6">
                {createdAt && (
                    <div className="flex flex-row items-center gap-2 md:items-start">
                        <CalendarDays className="size-4 shrink-0" />
                        <div className="flex flex-row flex-wrap items-baseline gap-x-1.5 text-left md:flex-col md:items-start md:gap-0">
                            <span>{t('avatar.memberSince')}</span>
                            <span className="font-medium text-foreground">
                                {new Intl.DateTimeFormat(i18n.language, {
                                    year: 'numeric',
                                    month: 'long',
                                    day: 'numeric',
                                }).format(new Date(createdAt))}
                            </span>
                        </div>
                    </div>
                )}
            </div>
        </div>
    );
}
