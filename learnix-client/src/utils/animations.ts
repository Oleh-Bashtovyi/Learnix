import type { Variants } from 'framer-motion';

export const fadeUpVariant: Variants = {
    initial: { opacity: 0, y: 30 },
    animate: {
        opacity: 1,
        y: 0,
        transition: { duration: 0.5, ease: [0.25, 0.1, 0.25, 1.0] }, // Smooth ease-out curve
    },
};

export const staggerContainer: Variants = {
    initial: {},
    animate: {
        transition: {
            staggerChildren: 0.15,
            delayChildren: 0.1,
        },
    },
};

export const viewportConfig = { once: true, margin: '-50px' };
