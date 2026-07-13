import { useTranslation } from 'react-i18next';
import { SearchX } from 'lucide-react';

export interface FaqSearchHit {
    /** The category the answer came from — a hit torn out of its section needs to say where it lived. */
    categoryTitle: string;
    q: string;
    a: string;
}

interface FaqSearchResultsProps {
    query: string;
    hits: FaqSearchHit[];
    onClear: () => void;
}

export function FaqSearchResults({ query, hits, onClear }: FaqSearchResultsProps) {
    const { t } = useTranslation('faq');

    if (hits.length === 0) {
        return (
            <div className="rounded-2xl border border-border bg-card p-10 text-center">
                <div className="mx-auto grid size-12 place-items-center rounded-full bg-muted text-muted-foreground">
                    <SearchX className="size-6" />
                </div>
                <h2 className="mt-4 font-heading text-lg font-semibold">
                    {t('search.emptyTitle', { query })}
                </h2>
                <p className="mt-1 text-sm text-muted-foreground">{t('search.emptySubtitle')}</p>
                <button
                    type="button"
                    onClick={onClear}
                    className="mt-4 text-sm font-medium text-link hover:underline"
                >
                    {t('search.clear')}
                </button>
            </div>
        );
    }

    return (
        <div>
            <p className="mb-4 text-sm text-muted-foreground">
                {t('search.resultsCount', { count: hits.length, query })}
            </p>

            <div className="space-y-2">
                {hits.map((hit) => (
                    <details
                        key={`${hit.categoryTitle}-${hit.q}`}
                        className="group rounded-xl border border-border bg-card"
                        open
                    >
                        <summary className="flex cursor-pointer list-none items-start justify-between rounded-xl p-4 hover:bg-secondary/30 [&::-webkit-details-marker]:hidden">
                            <span className="flex-1 pr-4 text-left">
                                <span className="mb-1 block text-xs font-medium uppercase tracking-wide text-muted-foreground">
                                    {hit.categoryTitle}
                                </span>
                                <Highlighted text={hit.q} query={query} className="font-medium" />
                            </span>
                            <span className="shrink-0 text-2xl font-light text-primary transition-transform duration-200 group-open:rotate-45">
                                +
                            </span>
                        </summary>
                        <div className="px-4 pb-4 text-sm leading-relaxed text-muted-foreground">
                            <Highlighted text={hit.a} query={query} />
                        </div>
                    </details>
                ))}
            </div>
        </div>
    );
}

interface HighlightedProps {
    text: string;
    query: string;
    className?: string;
}

/**
 * Marks every occurrence of the query in the text, so a hit shows *why* it is a hit — the phrase can
 * be buried a paragraph deep in an answer, and without this the reader has to hunt for it.
 * The query is escaped before it becomes a regex: it comes from an input, and `c++` would otherwise
 * throw rather than search.
 */
function Highlighted({ text, query, className }: HighlightedProps) {
    const escaped = query.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
    const parts = text.split(new RegExp(`(${escaped})`, 'gi'));

    return (
        <span className={className}>
            {parts.map((part, index) =>
                part.toLowerCase() === query.toLowerCase() ? (
                    <mark key={index} className="rounded bg-warning/25 text-foreground">
                        {part}
                    </mark>
                ) : (
                    <span key={index}>{part}</span>
                ),
            )}
        </span>
    );
}
