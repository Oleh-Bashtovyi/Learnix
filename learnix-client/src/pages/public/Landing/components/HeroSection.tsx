import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import { motion } from 'framer-motion';
import { useContinueLearning } from '@/hooks/student/useContinueLearning';
import { APP_ROUTES } from '@/routes/paths';
import { useAuthStore } from '@/store/auth.store';
import { fadeUpVariant, staggerContainer, viewportConfig } from '@/utils/animations';
import { isInstructorOrAdmin } from '@/utils/roles';

/** Decorative social-proof avatars; the seed picks the face the generator returns. */
const AVATAR_SEEDS = [15, 16, 17, 18];

export function HeroSection() {
    const { t } = useTranslation('landing');
    const user = useAuthStore((s) => s.user);
    const { data: continueLearning } = useContinueLearning();

    // Resume the course last worked on. With nothing in progress the catalog is the only useful
    // destination — "My learning" would be an empty page.
    const primaryCta = continueLearning
        ? {
              to: APP_ROUTES.student.learnLesson(
                  continueLearning.courseId,
                  continueLearning.lessonId,
              ),
              label: t('common:actions.continueLearning'),
          }
        : { to: APP_ROUTES.public.courses, label: t('common:actions.browseCourses') };

    return (
        <section className="relative overflow-hidden bg-background pb-32 pt-24">
            {/* Ambient Background Effects */}
            <div className="absolute inset-0 z-0">
                <div className="absolute left-[20%] top-[-30%] h-[600px] w-[600px] rounded-full bg-primary/20 mix-blend-screen blur-[120px]" />
                <div className="absolute right-[10%] top-[20%] h-[500px] w-[500px] rounded-full bg-accent/20 mix-blend-screen blur-[100px]" />
                <div className="absolute bottom-[-10%] left-[40%] h-[400px] w-[400px] rounded-full bg-warning/10 mix-blend-screen blur-[80px]" />
                <div className="absolute inset-0 bg-[url('/noise.png')] opacity-[0.03] mix-blend-overlay" />
                {/* Optional Grid pattern */}
                <div className="absolute inset-0 bg-[linear-gradient(to_right,#80808012_1px,transparent_1px),linear-gradient(to_bottom,#80808012_1px,transparent_1px)] bg-[size:24px_24px] [mask-image:radial-gradient(ellipse_60%_50%_at_50%_0%,#000_70%,transparent_100%)]" />
            </div>

            <motion.div
                variants={staggerContainer}
                initial="initial"
                whileInView="animate"
                viewport={viewportConfig}
                className="relative z-10 mx-auto grid max-w-7xl items-center gap-16 px-6 md:grid-cols-2 lg:gap-24"
            >
                <motion.div
                    variants={fadeUpVariant}
                    className="flex flex-col justify-center text-center md:text-left"
                >
                    <h1 className="font-heading text-4xl font-extrabold tracking-tight sm:text-5xl md:text-6xl lg:text-7xl lg:leading-[1.1]">
                        {t('hero.heading.line1')}
                        <br />
                        {t('hero.heading.line2')}{' '}
                        <span className="bg-gradient-to-r from-brand via-accent to-brand bg-clip-text text-transparent">
                            {t('hero.heading.highlight')}
                        </span>
                        .
                    </h1>
                    <p className="mx-auto mt-6 max-w-lg text-base leading-relaxed text-muted-foreground/90 sm:text-lg md:mx-0 md:text-xl">
                        {t('hero.subtitle')}
                    </p>
                    <div className="mt-10 flex flex-col flex-wrap justify-center gap-4 sm:flex-row md:justify-start">
                        <Link
                            to={primaryCta.to}
                            className="group relative inline-flex h-14 items-center justify-center overflow-hidden rounded-full bg-primary px-8 font-medium text-primary-foreground shadow-[0_0_40px_-10px_rgba(var(--primary),0.8)] transition-all hover:scale-[1.02] hover:shadow-[0_0_60px_-15px_rgba(var(--primary),0.8)] active:scale-[0.98]"
                        >
                            <div className="absolute inset-0 flex size-full justify-center [transform:skew(-12deg)_translateX(-100%)] group-hover:duration-1000 group-hover:[transform:skew(-12deg)_translateX(100%)]">
                                <div className="relative h-full w-8 bg-white/20" />
                            </div>
                            <span className="relative z-10 flex items-center gap-2">
                                {primaryCta.label}
                                <svg
                                    className="size-4 transition-transform group-hover:translate-x-1"
                                    fill="none"
                                    viewBox="0 0 24 24"
                                    stroke="currentColor"
                                >
                                    <path
                                        strokeLinecap="round"
                                        strokeLinejoin="round"
                                        strokeWidth={2}
                                        d="M14 5l7 7m0 0l-7 7m7-7H3"
                                    />
                                </svg>
                            </span>
                        </Link>
                        {!isInstructorOrAdmin(user) && (
                            <Link
                                to={APP_ROUTES.public.becomeInstructor}
                                className="inline-flex h-14 items-center justify-center rounded-full border border-white/10 bg-white/5 px-8 font-medium text-foreground backdrop-blur-md transition-all hover:bg-white/10 hover:shadow-lg active:scale-[0.98]"
                            >
                                {t('hero.cta.secondary')}
                            </Link>
                        )}
                    </div>

                    <div className="mt-12 flex flex-col items-center gap-5 sm:flex-row md:justify-start">
                        <div className="flex -space-x-3">
                            {AVATAR_SEEDS.map((seed, i) => (
                                <img
                                    key={seed}
                                    src={`https://api.dicebear.com/7.x/avataaars/svg?seed=${seed}&backgroundColor=b6e3f4,c0aede,d1d4f9`}
                                    alt="Learner avatar"
                                    // Tailwind cannot see an interpolated z-[…] class, so the stacking order is inline.
                                    style={{ zIndex: AVATAR_SEEDS.length - i }}
                                    className="size-10 rounded-full border-2 border-background bg-muted object-cover shadow-sm"
                                />
                            ))}
                            <div className="grid size-10 place-items-center rounded-full border-2 border-background bg-card text-xs font-bold text-foreground shadow-sm">
                                +85k
                            </div>
                        </div>
                        <div className="flex flex-col items-center sm:items-start">
                            <div className="flex text-warning">
                                {[...Array(5)].map((_, i) => (
                                    <svg
                                        key={i}
                                        className="size-4 fill-current"
                                        viewBox="0 0 24 24"
                                    >
                                        <path d="M12 17.27L18.18 21l-1.64-7.03L22 9.24l-7.19-.61L12 2 9.19 8.63 2 9.24l5.46 4.73L5.82 21z" />
                                    </svg>
                                ))}
                            </div>
                            <span className="mt-0.5 text-sm font-medium text-muted-foreground">
                                {t('hero.socialProof')}
                            </span>
                        </div>
                    </div>
                </motion.div>

                {/* Floating decorative cards — redesigned for better composition */}
                <motion.div
                    variants={fadeUpVariant}
                    className="relative mt-16 flex w-full flex-col items-center md:mt-0 md:items-end"
                >
                    <div className="relative w-full max-w-[540px]">
                        {/* AI Tutor Card - Top Right Corner - hidden on small mobile to avoid clutter */}
                        <div className="absolute -right-6 -top-8 z-20 hidden w-[290px] rounded-2xl border border-white/10 bg-card/95 p-4 shadow-[0_20px_40px_-15px_rgba(0,0,0,0.5)] backdrop-blur-xl transition-transform duration-500 hover:-translate-y-1 lg:block">
                            <div className="mb-4 flex items-center gap-2 text-[11px] font-semibold uppercase tracking-wider text-accent">
                                ✨ {t('hero.aiTutorCard.label')}
                            </div>
                            <div className="flex flex-col gap-3">
                                <div className="max-w-[90%] self-end rounded-2xl rounded-tr-sm bg-chat-user-bubble px-3 py-2 text-[13px] text-chat-user-bubble-foreground shadow-sm">
                                    {t('hero.aiTutorCard.question')}{' '}
                                    <code className="rounded bg-chat-user-bubble-foreground/20 px-1 py-0.5 font-mono text-[11px]">
                                        {t('hero.aiTutorCard.codeSnippet')}
                                    </code>{' '}
                                    {t('hero.aiTutorCard.questionEnd')}
                                </div>
                                <div className="max-w-[95%] self-start rounded-2xl rounded-tl-sm border border-border/50 bg-muted px-3 py-2.5 text-[13px] leading-relaxed text-foreground/90 shadow-sm">
                                    {t('hero.aiTutorCard.answer')}
                                </div>
                            </div>
                        </div>

                        {/* Main Video Card */}
                        <div className="relative z-10 h-[240px] w-full overflow-hidden rounded-2xl border border-white/10 shadow-2xl transition-transform duration-700 hover:-translate-y-1 sm:h-[340px]">
                            <div className="absolute inset-0 bg-[url('/video_thumbnail.png')] bg-cover bg-center" />
                            <div className="absolute inset-0 bg-gradient-to-t from-background/90 via-background/20 to-transparent opacity-80" />
                            <div className="absolute inset-0 flex items-center justify-center">
                                <div className="group flex size-20 cursor-pointer items-center justify-center rounded-full bg-white/10 shadow-[0_0_30px_rgba(255,255,255,0.1)] backdrop-blur-md transition-all duration-300 hover:scale-110 hover:bg-white/20">
                                    <svg
                                        className="ml-1 size-8 text-white drop-shadow-md transition-transform duration-300 group-hover:scale-110"
                                        fill="currentColor"
                                        viewBox="0 0 24 24"
                                    >
                                        <path d="M8 5v14l11-7z" />
                                    </svg>
                                </div>
                            </div>
                        </div>

                        {/* Grouped Bottom Cards */}
                        <div className="relative z-10 mt-6 grid grid-cols-1 gap-4 sm:grid-cols-2">
                            {/* Progress Card */}
                            <div className="rounded-2xl border border-white/10 bg-card/60 p-5 shadow-lg backdrop-blur-xl transition-transform duration-500 hover:-translate-y-1">
                                <p className="text-[10px] font-bold uppercase tracking-wider text-muted-foreground">
                                    {t('hero.progressCard.label')}
                                </p>
                                <p className="mt-1 truncate text-sm font-bold text-foreground">
                                    {t('hero.progressCard.title')}
                                </p>
                                <div className="mt-3 h-2 w-full overflow-hidden rounded-full bg-secondary/50 shadow-inner">
                                    <div
                                        className="relative h-full rounded-full bg-gradient-to-r from-brand to-accent"
                                        style={{ width: '68%' }}
                                    >
                                        <div className="absolute inset-0 animate-[progress_1s_linear_infinite] bg-[linear-gradient(45deg,rgba(255,255,255,0.15)_25%,transparent_25%,transparent_50%,rgba(255,255,255,0.15)_50%,rgba(255,255,255,0.15)_75%,transparent_75%,transparent)] bg-[length:1rem_1rem]" />
                                    </div>
                                </div>
                                <p className="mt-2 text-xs font-medium text-muted-foreground">
                                    {t('hero.progressCard.progress')}
                                </p>
                            </div>

                            {/* Achievement Card */}
                            <div className="flex flex-col justify-center rounded-2xl border border-white/10 bg-card/60 p-5 shadow-lg backdrop-blur-xl transition-transform duration-500 hover:-translate-y-1">
                                <div className="flex items-center gap-4">
                                    <div className="grid size-12 shrink-0 place-items-center rounded-xl border border-success/20 bg-gradient-to-br from-success/20 to-success/5 text-xl drop-shadow-sm">
                                        🏆
                                    </div>
                                    <div className="min-w-0">
                                        <p className="truncate text-[10px] font-bold uppercase tracking-wider text-muted-foreground">
                                            {t('hero.achievementCard.label')}
                                        </p>
                                        <p className="mt-0.5 truncate text-sm font-bold text-foreground">
                                            {t('hero.achievementCard.title')}
                                        </p>
                                        <p className="mt-0.5 text-xs font-semibold text-success">
                                            {t('hero.achievementCard.xp')}
                                        </p>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                </motion.div>
            </motion.div>
        </section>
    );
}
