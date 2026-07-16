import { useEffect, useRef, useState } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { queryKeys } from '@/api/queryKeys';

/**
 * Live countdown for a test's retake cooldown.
 *
 * The server reports the cooldown once, in whole minutes, as part of the test payload. Nothing else
 * ever revisits it: a view that just renders that number keeps claiming "1 min left" until the page
 * is reloaded by hand, long after the attempt became available. So the countdown is driven locally
 * and, on reaching zero, the test query is invalidated — the refetch is what actually flips
 * `canAttempt`, and it is the same refetch every consumer of this test is already subscribed to.
 *
 * @returns seconds remaining, or null when there is no cooldown to serve.
 */
export function useTestCooldown(
    courseId: string,
    lessonId: string,
    cooldownRemainingMinutes: number | null | undefined,
) {
    const queryClient = useQueryClient();
    const [secondsLeft, setSecondsLeft] = useState<number | null>(null);
    const intervalRef = useRef<ReturnType<typeof setInterval> | null>(null);

    useEffect(() => {
        if (intervalRef.current) {
            clearInterval(intervalRef.current);
            intervalRef.current = null;
        }

        if (!cooldownRemainingMinutes) {
            // eslint-disable-next-line react-hooks/set-state-in-effect
            setSecondsLeft(null);
            return;
        }

        setSecondsLeft(cooldownRemainingMinutes * 60);

        intervalRef.current = setInterval(() => {
            setSecondsLeft((prev) => {
                if (prev === null || prev <= 1) {
                    if (intervalRef.current) {
                        clearInterval(intervalRef.current);
                        intervalRef.current = null;
                    }
                    queryClient.invalidateQueries({
                        queryKey: queryKeys.tests.lesson(courseId, lessonId),
                    });
                    return null;
                }
                return prev - 1;
            });
        }, 1000);

        return () => {
            if (intervalRef.current) {
                clearInterval(intervalRef.current);
                intervalRef.current = null;
            }
        };
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [courseId, lessonId, cooldownRemainingMinutes]);

    return secondsLeft;
}

/** `135` -> `"02:15"`. The countdown is only ever shown as mm:ss. */
export function formatCooldown(seconds: number) {
    return {
        mm: String(Math.floor(seconds / 60)).padStart(2, '0'),
        ss: String(seconds % 60).padStart(2, '0'),
    };
}
