import typography from '@tailwindcss/typography';
import type { Config } from 'tailwindcss';

export default {
    content: ['./index.html', './src/**/*.{ts,tsx}'],
    darkMode: 'class',
    theme: {
        extend: {
            colors: {
                background: 'hsl(var(--background))',
                foreground: 'hsl(var(--foreground))',
                card: {
                    DEFAULT: 'hsl(var(--card))',
                    foreground: 'hsl(var(--card-foreground))',
                },
                popover: {
                    DEFAULT: 'hsl(var(--popover))',
                    foreground: 'hsl(var(--popover-foreground))',
                },
                primary: {
                    DEFAULT: 'hsl(var(--primary))',
                    foreground: 'hsl(var(--primary-foreground))',
                },
                brand: {
                    DEFAULT: 'hsl(var(--brand))',
                    foreground: 'hsl(var(--brand-foreground))',
                },
                panel: {
                    DEFAULT: 'hsl(var(--panel))',
                    foreground: 'hsl(var(--panel-foreground))',
                },
                link: 'hsl(var(--link))',
                secondary: {
                    DEFAULT: 'hsl(var(--secondary))',
                    foreground: 'hsl(var(--secondary-foreground))',
                },
                muted: {
                    DEFAULT: 'hsl(var(--muted))',
                    foreground: 'hsl(var(--muted-foreground))',
                },
                accent: {
                    // Fills only (bg-accent, gradients, tints). For accent-coloured *text* and
                    // icons use `strong` — see --accent / --accent-strong in styles/index.css.
                    DEFAULT: 'hsl(var(--accent))',
                    foreground: 'hsl(var(--accent-foreground))',
                    strong: 'hsl(var(--accent-strong))',
                },
                // The neutral hover/focus surface for interactive elements. Distinct from
                // `accent`, which is the brand colour — see --hover in styles/index.css.
                hover: {
                    DEFAULT: 'hsl(var(--hover))',
                    foreground: 'hsl(var(--hover-foreground))',
                },
                destructive: {
                    DEFAULT: 'hsl(var(--destructive))',
                    foreground: 'hsl(var(--destructive-foreground))',
                },
                success: 'hsl(var(--success))',
                warning: 'hsl(var(--warning))',
                achievement: 'hsl(var(--achievement))',
                bestseller: {
                    DEFAULT: 'hsl(var(--bestseller))',
                    foreground: 'hsl(var(--bestseller-foreground))',
                },
                border: 'hsl(var(--border))',
                input: 'hsl(var(--input))',
                ring: 'hsl(var(--ring))',
                // Form-field palette — see --field-* in styles/index.css. Every input,
                // select, textarea, checkbox and radio draws from these; nothing else should.
                field: {
                    DEFAULT: 'hsl(var(--field))',
                    hover: 'hsl(var(--field-hover))',
                    card: 'hsl(var(--field-card))',
                    'card-hover': 'hsl(var(--field-card-hover))',
                    border: 'hsl(var(--field-border))',
                    focus: 'hsl(var(--field-border-focus))',
                    error: 'hsl(var(--field-border-error))',
                    accent: 'hsl(var(--field-accent))',
                },
                'chat-user-bubble': {
                    DEFAULT: 'hsl(var(--chat-user-bubble))',
                    foreground: 'hsl(var(--chat-user-bubble-foreground))',
                },
            },
            fontFamily: {
                sans: ['"DM Sans"', 'system-ui', 'sans-serif'],
                heading: ['"Plus Jakarta Sans"', '"DM Sans"', 'sans-serif'],
            },
            borderRadius: {
                lg: 'var(--radius)',
                md: 'calc(var(--radius) - 2px)',
                sm: 'calc(var(--radius) - 4px)',
            },
        },
    },
    plugins: [typography],
} satisfies Config;
