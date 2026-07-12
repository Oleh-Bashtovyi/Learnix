import type { CSSProperties, HTMLAttributes } from 'react';
import Markdown, { type Components } from 'react-markdown';
import { Link } from 'react-router-dom';
import { Prism as SyntaxHighlighter } from 'react-syntax-highlighter';
import { vscDarkPlus } from 'react-syntax-highlighter/dist/esm/styles/prism';
import { cn } from '@/utils/cn';

const SAFE_PROTOCOL_RE = /^(https?:\/\/|mailto:|tel:)/i;

const safeComponents: Components = {
    a: ({ href, children }) => {
        if (!href) return <span>{children}</span>;

        if (href.startsWith('/')) {
            return (
                <Link to={href} className="font-medium text-link hover:underline">
                    {children}
                </Link>
            );
        }

        if (!SAFE_PROTOCOL_RE.test(href)) return <span>{children}</span>;

        return (
            <a
                href={href}
                target="_blank"
                rel="noopener noreferrer"
                className="font-medium text-link hover:underline"
            >
                {children}
            </a>
        );
    },
    code: ({ className, children, node: _node, ...props }) => {
        const match = /language-(\w+)/.exec(className || '');
        const isInline = !match && !className?.includes('language-');

        if (!isInline && match) {
            return (
                <SyntaxHighlighter
                    style={vscDarkPlus as { [key: string]: CSSProperties }}
                    language={match[1]}
                    PreTag="div"
                    className="not-prose !my-2 max-w-full rounded-md"
                    wrapLines={true}
                    wrapLongLines={true}
                    customStyle={{ whiteSpace: 'pre-wrap', wordBreak: 'break-word' }}
                    codeTagProps={{ style: { whiteSpace: 'pre-wrap', wordBreak: 'break-word' } }}
                    {...(props as Omit<HTMLAttributes<HTMLElement>, 'style'>)}
                >
                    {String(children).replace(/\n$/, '')}
                </SyntaxHighlighter>
            );
        }

        return (
            <code className={className} {...props}>
                {children}
            </code>
        );
    },
};

interface MarkdownRendererProps {
    content: string;
    className?: string;
}

export function MarkdownRenderer({ content, className }: MarkdownRendererProps) {
    return (
        <div className={cn('prose prose-neutral max-w-none dark:prose-invert', className)}>
            <Markdown components={safeComponents}>{content}</Markdown>
        </div>
    );
}
