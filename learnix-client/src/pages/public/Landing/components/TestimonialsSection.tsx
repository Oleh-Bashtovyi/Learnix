import { useTranslation } from 'react-i18next';
import { motion } from 'framer-motion';
import { fadeUpVariant, staggerContainer, viewportConfig } from '@/utils/animations';

export function TestimonialsSection() {
    const { t } = useTranslation('landing');
    const items = t('testimonials.items', { returnObjects: true }) as Array<{
        name: string;
        role: string;
        text: string;
        rating: number;
        initials: string;
        avatarBg: string;
    }>;

    return (
        <section className="py-12 md:py-20">
            <div className="mx-auto max-w-7xl px-6">
                <div className="mb-10 text-center md:mb-14">
                    <h2 className="mt-2 font-heading text-3xl font-bold md:text-4xl">
                        {t('testimonials.heading')}
                    </h2>
                    <p className="mt-3 text-muted-foreground">{t('testimonials.subtitle')}</p>
                </div>

                <motion.div
                    variants={staggerContainer}
                    initial="initial"
                    whileInView="animate"
                    viewport={viewportConfig}
                    className="grid gap-6 md:grid-cols-3"
                >
                    {items.map((t_item) => (
                        <motion.div
                            variants={fadeUpVariant}
                            key={t_item.name}
                            className="rounded-xl border border-border bg-card p-6"
                        >
                            <div className="mb-3 text-warning">
                                {'★'.repeat(t_item.rating)}
                                {'☆'.repeat(5 - t_item.rating)}
                            </div>
                            <p className="leading-relaxed text-foreground">"{t_item.text}"</p>
                            <div className="mt-5 flex items-center gap-3 border-t border-border pt-5">
                                <div
                                    className={`grid size-10 place-items-center rounded-full ${t_item.avatarBg} text-sm font-medium`}
                                >
                                    {t_item.initials}
                                </div>
                                <div>
                                    <p className="text-sm font-medium">{t_item.name}</p>
                                    <p className="text-xs text-muted-foreground">{t_item.role}</p>
                                </div>
                            </div>
                        </motion.div>
                    ))}
                </motion.div>
            </div>
        </section>
    );
}
