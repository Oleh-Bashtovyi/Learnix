import React, { useRef } from 'react';
import { useTranslation } from 'react-i18next';
import { CalendarDays, Camera, User } from 'lucide-react';
import { ChangePasswordDialog } from './ChangePasswordDialog';

interface AvatarUploadProps {
    firstName?: string;
    lastName?: string;
    email?: string;
    hasPassword?: boolean;
    displayAvatar: string | null;
    isUploading: boolean;
    createdAt?: string;
    onAvatarChange: (e: React.ChangeEvent<HTMLInputElement>) => void;
}

export function AvatarUpload({
    firstName,
    lastName,
    email,
    hasPassword,
    displayAvatar,
    isUploading,
    createdAt,
    onAvatarChange,
}: AvatarUploadProps) {
    const { t, i18n } = useTranslation('profile');
    const fileInputRef = useRef<HTMLInputElement>(null);

    return (
        <div className="flex shrink-0 flex-col items-center text-center md:w-48 md:items-start md:text-left">
            <div className="relative">
                <div className="flex size-28 items-center justify-center overflow-hidden rounded-full border-4 border-background bg-muted shadow-sm md:size-32">
                    {displayAvatar !== null ? (
                        <img src={displayAvatar} alt="Avatar" className="size-full object-cover" />
                    ) : (
                        <User className="size-12 text-muted-foreground md:size-14" />
                    )}
                </div>
                <button
                    type="button"
                    onClick={() => fileInputRef.current?.click()}
                    disabled={isUploading}
                    title={isUploading ? t('avatar.uploading') : t('avatar.upload')}
                    className="absolute bottom-1 right-1 flex size-9 items-center justify-center rounded-full border-2 border-background bg-accent text-accent-foreground shadow-md transition-transform hover:scale-105 hover:opacity-90 disabled:scale-100 disabled:opacity-50"
                >
                    <Camera className="size-4" />
                </button>
                <input
                    ref={fileInputRef}
                    type="file"
                    accept="image/jpeg,image/png,image/webp"
                    className="hidden"
                    onChange={onAvatarChange}
                />
            </div>
            <div className="mt-4">
                <h3 className="font-heading text-lg font-semibold text-foreground">
                    {firstName} {lastName}
                </h3>
                <p className="mt-1 max-w-[200px] text-xs text-muted-foreground">
                    {t('avatar.hint')}
                </p>
            </div>

            <div className="mt-8 flex w-full flex-col gap-4 border-t border-border pt-6 text-sm text-muted-foreground md:items-start">
                {createdAt && (
                    <div className="flex items-start gap-2">
                        <CalendarDays className="mt-0.5 size-4 shrink-0" />
                        <div className="flex flex-col">
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

                <ChangePasswordDialog hasPassword={hasPassword} email={email} />
            </div>
        </div>
    );
}
