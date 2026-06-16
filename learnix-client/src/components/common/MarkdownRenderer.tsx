import Markdown, { type Components } from 'react-markdown';
import { cn } from '@/utils/cn';

const safeComponents: Components = {
    a: ({ href, children }) => {
        if (!href?.match(/^(https?:\/\/|mailto:|tel:)/i)) return <span>{children}</span>;
        return (
            <a href={href} target="_blank" rel="noopener noreferrer">
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
