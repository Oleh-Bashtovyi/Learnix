import { Link } from 'react-router-dom';
import {
    TwitterIcon,
    GitHubIcon,
    LinkedInIcon,
    YouTubeIcon,
} from '@/components/common/icons/SocialIcons';

interface FooterLink {
    label: string;
    to: string;
    external?: boolean;
}

const productLinks: FooterLink[] = [
    { label: 'Browse courses', to: '/courses' },
    { label: 'Categories', to: '/courses' },
    { label: 'AI Tutor', to: '/#features', external: true },
    { label: 'Certificates', to: '/certificates' },
    { label: 'Achievements', to: '/achievements' },
];

const teachLinks: FooterLink[] = [
    { label: 'Become instructor', to: '/become-instructor' },
    { label: 'Instructor handbook', to: '#', external: true },
    { label: 'Revenue & payouts', to: '#', external: true },
    { label: 'Course guidelines', to: '#', external: true },
];

const companyLinks: FooterLink[] = [
    { label: 'About', to: '#', external: true },
    { label: 'Blog', to: '#', external: true },
    { label: 'FAQ', to: '/faq' },
    { label: 'Contact', to: '#', external: true },
    { label: 'Status', to: '#', external: true },
];

const legalLinks = ['Privacy', 'Terms', 'Cookies', 'Accessibility'];

const socialLinks = [
    { name: 'twitter', Icon: TwitterIcon },
    { name: 'github', Icon: GitHubIcon },
    { name: 'linkedin', Icon: LinkedInIcon },
    { name: 'youtube', Icon: YouTubeIcon },
];

function renderLink(link: FooterLink) {
    const className = 'hover:text-primary';
    if (link.external) {
        return (
            <a key={link.label} href={link.to} className={className}>
                {link.label}
            </a>
        );
    }
    return (
        <Link key={link.label} to={link.to} className={className}>
            {link.label}
        </Link>
    );
}

export function Footer() {
    return (
        <footer className="border-t border-border bg-card pb-8 pt-16">
            <div className="mx-auto max-w-7xl px-6">
                <div className="grid gap-10 border-b border-border pb-12 md:grid-cols-5">
                    <div className="md:col-span-2">
                        <Link to="/" className="flex items-center gap-2">
                            <div className="grid h-8 w-8 place-items-center rounded-lg bg-primary font-heading font-bold text-primary-foreground">
                                L
                            </div>
                            <span className="font-heading text-lg font-bold">Learnix</span>
                        </Link>
                        <p className="mt-4 max-w-xs text-sm leading-relaxed text-muted-foreground">
                            A modern learning platform built around the way developers actually
                            learn — with AI assistance, real projects, and lifetime access.
                        </p>
                        <div className="mt-6 flex gap-3">
                            {socialLinks.map(({ name, Icon }) => (
                                <a
                                    key={name}
                                    href="#"
                                    aria-label={name}
                                    className="grid h-9 w-9 place-items-center rounded-lg border border-border text-muted-foreground hover:bg-secondary hover:text-primary"
                                >
                                    <Icon className="h-4 w-4" />
                                </a>
                            ))}
                        </div>
                    </div>

                    <div>
                        <h4 className="mb-4 font-heading text-sm font-semibold">Product</h4>
                        <ul className="space-y-3 text-sm text-muted-foreground">
                            {productLinks.map((l) => (
                                <li key={l.label}>{renderLink(l)}</li>
                            ))}
                        </ul>
                    </div>

                    <div>
                        <h4 className="mb-4 font-heading text-sm font-semibold">Teach</h4>
                        <ul className="space-y-3 text-sm text-muted-foreground">
                            {teachLinks.map((l) => (
                                <li key={l.label}>{renderLink(l)}</li>
                            ))}
                        </ul>
                    </div>

                    <div>
                        <h4 className="mb-4 font-heading text-sm font-semibold">Company</h4>
                        <ul className="space-y-3 text-sm text-muted-foreground">
                            {companyLinks.map((l) => (
                                <li key={l.label}>{renderLink(l)}</li>
                            ))}
                        </ul>
                    </div>
                </div>

                <div className="flex flex-col items-start justify-between gap-4 pt-8 text-sm text-muted-foreground md:flex-row md:items-center">
                    <div>
                        © 2026 Learnix. Portfolio project — not affiliated with any commercial LMS.
                    </div>
                    <div className="flex gap-6">
                        {legalLinks.map((label) => (
                            <a key={label} href="#" className="hover:text-primary">
                                {label}
                            </a>
                        ))}
                    </div>
                </div>
            </div>
        </footer>
    );
}
