import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { CalendarDays, Star, Users } from 'lucide-react';
import { HeroPanel } from '@/components/common/ui/HeroPanel';
import { RatingStars } from '@/components/common/ui/RatingStars';
import { StatTile } from '@/components/common/ui/StatTile';
import type { InstructorProfileDto } from '@/types/user.types';
import { cn } from '@/utils/cn';

interface InstructorHeroProps {
    profile: InstructorProfileDto;
    fullName: string;
}

/**
 * Roughly how much bio fits in the three lines it is clamped to. Used only to decide whether the
 * expand toggle is worth showing — a control that unfolds nothing is worse than no control. A
 * character count is a guess where a measured height would be exact, but it is a guess that costs no
 * layout pass, and being one line out only means the toggle appears when it need not have.
 */
const BIO_CLAMP_APPROX_CHARS = 200;

export function InstructorHero({ profile, fullName }: InstructorHeroProps) {
    const { t, i18n } = useTranslation('instructorProfile');
    const [isBioExpanded, setIsBioExpanded] = useState(false);

    const hasReviews = profile.reviewsCount > 0;
    const isBioLong = (profile.bio?.length ?? 0) > BIO_CLAMP_APPROX_CHARS;
    const initials = `${profile.firstName.charAt(0)}${profile.lastName.charAt(0)}`.toUpperCase();

    return (
        <HeroPanel className="mb-10">
            <div className="flex flex-col gap-5 sm:flex-row sm:items-start sm:gap-6">
                {/* The avatar sits in a gradient ring rather than a flat border, and falls back to
                        initials — a generic silhouette says nothing, and every instructor has a name.

                        `self-start` is load-bearing: the row is a column on a phone, and a flex column
                        stretches its children across the full width by default. The ring is a rounded
                        wrapper with no width of its own, so it obligingly became a full-width pill. */}
                <div className="shrink-0 self-start rounded-full bg-gradient-to-br from-brand to-accent p-[2px]">
                    <div className="rounded-full bg-card p-1">
                        {profile.avatarUrl ? (
                            <img
                                src={profile.avatarUrl}
                                alt={fullName}
                                className="size-20 rounded-full object-cover sm:size-24"
                            />
                        ) : (
                            <div className="grid size-20 place-items-center rounded-full bg-gradient-to-br from-brand/15 to-accent/15 font-heading text-2xl font-bold text-foreground sm:size-24">
                                {initials}
                            </div>
                        )}
                    </div>
                </div>

                <div className="min-w-0 flex-1">
                    <h1 className="font-heading text-2xl font-bold text-foreground sm:text-3xl">
                        {fullName}
                    </h1>

                    <div className="mt-2 flex flex-wrap items-center gap-x-4 gap-y-1.5 text-sm text-muted-foreground">
                        <span>{t('coursesCount', { count: profile.coursesCount })}</span>
                        {hasReviews && (
                            <span className="flex items-center gap-1.5">
                                <RatingStars value={profile.averageRating} size="sm" />
                                <span className="font-medium text-foreground">
                                    {profile.averageRating.toFixed(1)}
                                </span>
                            </span>
                        )}
                    </div>

                    {profile.bio && (
                        <div className="mt-4 max-w-2xl">
                            <p
                                className={cn(
                                    'whitespace-pre-line text-sm leading-relaxed text-muted-foreground',
                                    isBioLong && !isBioExpanded && 'line-clamp-3',
                                )}
                            >
                                {profile.bio}
                            </p>
                            {isBioLong && (
                                <button
                                    type="button"
                                    onClick={() => setIsBioExpanded((v) => !v)}
                                    className="mt-1 text-xs font-medium text-link transition-colors hover:underline"
                                >
                                    {isBioExpanded
                                        ? t('common:actions.showLess')
                                        : t('common:actions.showMore')}
                                </button>
                            )}
                        </div>
                    )}
                </div>
            </div>

            <dl className="mt-6 grid grid-cols-1 gap-3 sm:grid-cols-3">
                <StatTile
                    icon={<Users className="size-5" />}
                    tone="brand"
                    label={t('stats.students')}
                    value={profile.totalStudents.toLocaleString(i18n.language)}
                />
                <StatTile
                    icon={<Star className="size-5" />}
                    tone="accent"
                    label={t('stats.rating')}
                    value={hasReviews ? profile.averageRating.toFixed(1) : '—'}
                    hint={
                        hasReviews
                            ? t('stats.reviews', { count: profile.reviewsCount })
                            : t('stats.ratingEmpty')
                    }
                />
                <StatTile
                    icon={<CalendarDays className="size-5" />}
                    tone="success"
                    label={t('stats.joined')}
                    value={new Date(profile.joinedAt).toLocaleDateString(i18n.language, {
                        year: 'numeric',
                        month: 'long',
                    })}
                />
            </dl>
        </HeroPanel>
    );
}
