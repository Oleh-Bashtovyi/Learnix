import { useEffect, useState } from 'react';

const COOLDOWN_KEY = 'learnix_email_resend_timestamp';
const COOLDOWN_SECONDS = 60;

export function useEmailResendCooldown() {
    const [secondsRemaining, setSecondsRemaining] = useState(0);

    useEffect(() => {
        const checkCooldown = () => {
            const storedTimestamp = localStorage.getItem(COOLDOWN_KEY);
            if (storedTimestamp) {
                const timestamp = parseInt(storedTimestamp, 10);
                const elapsed = Math.floor((Date.now() - timestamp) / 1000);
                if (elapsed < COOLDOWN_SECONDS) {
                    setSecondsRemaining(COOLDOWN_SECONDS - elapsed);
                } else {
                    setSecondsRemaining(0);
                }
            }
        };

        checkCooldown();
    }, []);

    useEffect(() => {
        if (secondsRemaining <= 0) return;

        const interval = setInterval(() => {
            setSecondsRemaining((prev) => {
                if (prev <= 1) {
                    clearInterval(interval);
                    return 0;
                }
                return prev - 1;
            });
        }, 1000);

        return () => clearInterval(interval);
    }, [secondsRemaining]);

    const startCooldown = () => {
        localStorage.setItem(COOLDOWN_KEY, Date.now().toString());
        setSecondsRemaining(COOLDOWN_SECONDS);
    };

    return {
        isCoolingDown: secondsRemaining > 0,
        secondsRemaining,
        startCooldown,
    };
}
