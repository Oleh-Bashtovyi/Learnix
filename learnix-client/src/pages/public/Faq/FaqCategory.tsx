import { cn } from '@/utils/cn';

interface FaqCategoryData {
    id: string;
    title: string;
    subtitle: string;
    icon: string;
    colorClass: string;
    items: Array<{ q: string; a: string }>;
}

interface FaqCategoryProps {
    category: FaqCategoryData | object;
    isFirst?: boolean;
}

export function FaqCategory({ category, isFirst = false }: FaqCategoryProps) {
    const cat = category as FaqCategoryData;

    return (
        <div id={cat.id}>
            <div className="mb-5 flex items-center gap-3">
                <div
                    className={cn(
                        'grid h-10 w-10 place-items-center rounded-lg text-xl',
                        cat.colorClass,
                    )}
                >
                    {cat.icon}
                </div>
                <div>
                    <h2 className="font-heading text-2xl font-bold">{cat.title}</h2>
                    <p className="text-sm text-muted-foreground">{cat.subtitle}</p>
                </div>
            </div>
            <div className="space-y-2">
                {cat.items?.map((item, index) => (
                    <details
                        key={index}
                        className="group rounded-xl border border-border bg-card"
                        open={isFirst && index === 0}
                    >
                        <summary className="flex cursor-pointer list-none items-center justify-between rounded-xl p-4 hover:bg-secondary/30 [&::-webkit-details-marker]:hidden">
                            <span className="pr-4 font-medium">{item.q}</span>
                            <span className="faq-icon shrink-0 text-2xl font-light text-primary transition-transform duration-200 group-open:rotate-45">
                                +
                            </span>
                        </summary>
                        <div className="px-4 pb-4 text-sm leading-relaxed text-muted-foreground">
                            {item.a}
                        </div>
                    </details>
                ))}
            </div>
        </div>
    );
}
