import Markdown, { type Components } from 'react-markdown';
import { Link } from 'react-router-dom';
import { cn } from '@/utils/cn';

const safeComponents: Components = {
    a: ({ href, children }) => {
        if (!href) return <span>{children}</span>;

        if (href.startsWith('/')) {
            return (
                <Link to={href} className="font-medium text-primary hover:underline">
                    {children}
                </Link>
            );
        }

        if (!href.match(/^(https?:\/\/|mailto:|tel:)/i)) return <span>{children}</span>;

        return (
            <a
                href={href}
                target="_blank"
                rel="noopener noreferrer"
                className="font-medium text-primary hover:underline"
            >
                {children}
            </a>
        );
    },
};

interface MarkdownRendererProps {
    content: string;
    className?: string;
}

export function MarkdownRenderer({ content, className }: MarkdownRendererProps) {
    return (
        <div className={cn('prose prose-neutral dark:prose-invert max-w-none', className)}>
            <Markdown components={safeComponents}>{content}</Markdown>
        </div>
    );
}
