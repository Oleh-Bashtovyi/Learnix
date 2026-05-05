import { LANDING_PAGE } from '@/const/localization/landingPage';

const { AI_ASSISTANT } = LANDING_PAGE;

export function AIAssistantSection() {
    return (
        <section id="features" className="bg-foreground py-20 text-background">
            <div className="mx-auto grid max-w-7xl items-center gap-12 px-6 md:grid-cols-2">
                <div>
                    <span className="inline-flex items-center gap-2 rounded-full bg-accent/20 px-3 py-1.5 text-xs font-semibold uppercase tracking-wider text-accent">
                        {AI_ASSISTANT.badge}
                    </span>
                    <h2 className="mt-5 font-heading text-4xl font-bold leading-tight md:text-5xl">
                        {AI_ASSISTANT.heading.line1}
                        <br />
                        {AI_ASSISTANT.heading.line2}
                        <br />
                        <span className="text-primary">{AI_ASSISTANT.heading.highlight}</span>
                    </h2>
                    <p className="mt-6 max-w-lg text-lg leading-relaxed text-background/70">
                        {AI_ASSISTANT.subtitle}
                    </p>
                    <ul className="mt-8 space-y-4 text-background/80">
                        {AI_ASSISTANT.features.map((f) => (
                            <li key={f} className="flex gap-3">
                                <span className="mt-0.5 text-success">✓</span>
                                <span>{f}</span>
                            </li>
                        ))}
                    </ul>
                </div>

                {/* Mock chat panel */}
                <div className="overflow-hidden rounded-2xl border border-border bg-card text-foreground shadow-2xl">
                    <div className="flex items-center gap-3 border-b border-border p-4">
                        <div className="grid h-10 w-10 place-items-center rounded-full bg-accent/20 text-accent">
                            ✨
                        </div>
                        <div>
                            <p className="text-sm font-medium">{AI_ASSISTANT.chat.title}</p>
                            <p className="flex items-center gap-1 text-xs text-muted-foreground">
                                <span className="h-1.5 w-1.5 rounded-full bg-success" />
                                {AI_ASSISTANT.chat.status}
                            </p>
                        </div>
                    </div>

                    <div className="max-h-[420px] min-h-[340px] space-y-4 overflow-y-auto p-5">
                        <div className="flex gap-3">
                            <div className="grid h-8 w-8 shrink-0 place-items-center rounded-full bg-primary/20 text-xs font-medium">
                                {AI_ASSISTANT.chat.userLabel}
                            </div>
                            <div className="max-w-[80%] rounded-xl rounded-tl-sm bg-secondary p-3 text-sm">
                                {AI_ASSISTANT.chat.messages.q1}{' '}
                                <code className="rounded bg-card px-1 py-0.5 text-xs">
                                    {AI_ASSISTANT.chat.messages.q1Code}
                                </code>{' '}
                                {AI_ASSISTANT.chat.messages.q1End}
                            </div>
                        </div>
                        <div className="flex gap-3">
                            <div className="grid h-8 w-8 shrink-0 place-items-center rounded-full bg-accent/20 text-xs text-accent">
                                {AI_ASSISTANT.chat.aiLabel}
                            </div>
                            <div className="max-w-[80%] rounded-xl rounded-tl-sm bg-primary/10 p-3 text-sm">
                                {AI_ASSISTANT.chat.messages.a1}{' '}
                                <strong>{AI_ASSISTANT.chat.messages.a1Bold}</strong>{' '}
                                {AI_ASSISTANT.chat.messages.a1End}
                                <div className="mt-2 text-xs text-muted-foreground">
                                    {AI_ASSISTANT.chat.messages.a1Note}
                                </div>
                            </div>
                        </div>
                        <div className="flex gap-3">
                            <div className="grid h-8 w-8 shrink-0 place-items-center rounded-full bg-primary/20 text-xs font-medium">
                                {AI_ASSISTANT.chat.userLabel}
                            </div>
                            <div className="max-w-[80%] rounded-xl rounded-tl-sm bg-secondary p-3 text-sm">
                                {AI_ASSISTANT.chat.messages.q2}
                            </div>
                        </div>
                        <div className="flex gap-3">
                            <div className="grid h-8 w-8 shrink-0 place-items-center rounded-full bg-accent/20 text-xs text-accent">
                                {AI_ASSISTANT.chat.aiLabel}
                            </div>
                            <div className="max-w-[80%] rounded-xl rounded-tl-sm bg-primary/10 p-3 text-sm">
                                <span className="inline-flex gap-1">
                                    <span className="h-1.5 w-1.5 animate-pulse rounded-full bg-accent" />
                                    <span
                                        className="h-1.5 w-1.5 animate-pulse rounded-full bg-accent"
                                        style={{ animationDelay: '0.2s' }}
                                    />
                                    <span
                                        className="h-1.5 w-1.5 animate-pulse rounded-full bg-accent"
                                        style={{ animationDelay: '0.4s' }}
                                    />
                                </span>
                            </div>
                        </div>
                    </div>

                    <div className="border-t border-border bg-secondary/30 p-4">
                        <div className="flex items-center gap-2 rounded-lg border border-border bg-card px-3 py-2">
                            <input
                                type="text"
                                placeholder={AI_ASSISTANT.chat.inputPlaceholder}
                                className="flex-1 bg-transparent text-sm outline-none"
                            />
                            <button type="button" className="text-primary">
                                ↑
                            </button>
                        </div>
                    </div>
                </div>
            </div>
        </section>
    );
}
