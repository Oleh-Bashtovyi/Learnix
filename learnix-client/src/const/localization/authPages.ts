export const AUTH_PAGES = {
    LOGIN: {
        title: 'Welcome back',
        subtitle: 'Enter your credentials to continue learning',
        email: {
            label: 'Email address',
            placeholder: 'you@example.com',
        },
        password: {
            label: 'Password',
            placeholder: '••••••••',
        },
        forgotPassword: 'Forgot password?',
        submit: 'Log in',
        submitting: 'Logging in...',
        divider: 'or continue with',
        google: 'Continue with Google',
        googleError: 'Google sign-in failed. Please try again.',
        noAccount: "Don't have an account?",
        register: 'Sign up',
        successRedirect: 'Login successful',
    },
    REGISTER: {
        title: 'Create your account',
        subtitle: 'Join thousands of learners on Learnix',
        firstName: {
            label: 'First name',
            placeholder: 'John',
        },
        lastName: {
            label: 'Last name',
            placeholder: 'Doe',
        },
        email: {
            label: 'Email address',
            placeholder: 'you@example.com',
        },
        password: {
            label: 'Password',
            placeholder: '••••••••',
            requirements: {
                heading: 'Password must:',
                minLength: 'Be at least 8 characters',
                uppercase: 'Contain an uppercase letter',
                lowercase: 'Contain a lowercase letter',
                digit: 'Contain a digit',
            },
        },
        confirmPassword: {
            label: 'Confirm password',
            placeholder: '••••••••',
        },
        submit: 'Create account',
        submitting: 'Creating account...',
        divider: 'or continue with',
        google: 'Sign up with Google',
        googleError: 'Google sign-up failed. Please try again.',
        hasAccount: 'Already have an account?',
        login: 'Log in',
        success: {
            title: 'Check your inbox!',
            message:
                "We've sent a confirmation email to {email}. Click the link in the email to activate your account.",
            resend: 'Resend email',
            resendCooldown: 'Resend in {seconds}s',
            login: 'Back to log in',
        },
    },
} as const;
