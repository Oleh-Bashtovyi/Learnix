export const EMAIL_CONFIRMATION = {
    BANNER: {
        MESSAGE: 'Please confirm your email address to unlock all platform features.',
        RESEND: 'Resend email',
        RESEND_COOLDOWN: 'Resend in {seconds}s',
    },
    VERIFY: {
        VERIFYING_TITLE: 'Confirming your email…',
        VERIFYING_SUBTITLE: 'Please wait a moment.',
        SUCCESS_TITLE: 'Email confirmed!',
        SUCCESS_SUBTITLE: 'Your account is now fully activated. You can log in.',
        SUCCESS_CTA: 'Go to login',
        ERROR_TITLE: 'Confirmation failed',
        ERROR_SUBTITLE: 'The link may have expired or already been used.',
        ERROR_CTA: 'Back to login',
        RESEND_HINT: 'Need a new link?',
        RESEND: 'Resend confirmation email',
    },
    PROFILE: {
        SECTION_TITLE: 'Email confirmation',
        NOT_CONFIRMED: 'Your email address is not confirmed yet.',
        NOT_CONFIRMED_HINT:
            'Some features (enrolling in courses, submitting reviews) require a confirmed email.',
        RESEND: 'Resend confirmation email',
        RESEND_COOLDOWN: 'Resend in {seconds}s',
        CONFIRMED: 'Email confirmed',
    },
} as const;
