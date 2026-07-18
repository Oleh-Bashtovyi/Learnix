import type { CSSProperties, HTMLAttributes } from 'react';
import Markdown, { type Components } from 'react-markdown';
import { Link } from 'react-router-dom';
import { Prism as SyntaxHighlighter } from 'react-syntax-highlighter';
import { vscDarkPlus } from 'react-syntax-highlighter/dist/esm/styles/prism';
import { cn } from '@/utils/cn';

const SAFE_PROTOCOL_RE = /^(https?:\/\/|mailto:|tel:)/i;

const safeComponents: Components = {
    // react-markdown wraps code blocks in <pre>. We strip it so prose doesn't apply its own black background,
    // and rely entirely on SyntaxHighlighter to provide the dark-grey block background.
    pre: ({ children }) => <>{children}</>,
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
        // Inline code shouldn't have newlines. Fenced code or 4-space indented code will.
        const hasNewlines = String(children).includes('\n');
        const isBlock = match || hasNewlines;

        if (isBlock) {
            return (
                <SyntaxHighlighter
                    style={vscDarkPlus as { [key: string]: CSSProperties }}
                    language={match ? match[1] : 'text'}
                    PreTag="div"
                    className="not-prose !my-4 max-w-full rounded-md"
                    wrapLines={true}
                    wrapLongLines={true}
                    customStyle={{ whiteSpace: 'pre-wrap', wordBreak: 'break-word', margin: 0 }}
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

/**
 * Authored markdown is always body content: every call site renders it inside a page that already
 * carries its own `text-2xl` heading. At the typography plugin's defaults an authored `#` comes out
 * at 36px and a `##` at 24px, so the first outranks that heading and the second ties it — the
 * instructor's prose then reads as the name of the page. The ladder is capped below `text-2xl`
 * instead. A caller that genuinely is the page can raise it back: `className` is merged last.
 */
const NESTED_HEADINGS =
    'prose-headings:font-heading prose-h1:text-lg prose-h2:text-base prose-h3:text-sm prose-h4:text-sm';

interface MarkdownRendererProps {
    content: string;
    className?: string;
}

export function MarkdownRenderer({ content, className }: MarkdownRendererProps) {
    return (
        <div
            className={cn(
                'prose prose-neutral max-w-none dark:prose-invert',
                NESTED_HEADINGS,
                className,
            )}
        >
            <Markdown components={safeComponents}>{content}</Markdown>
        </div>
    );
}
