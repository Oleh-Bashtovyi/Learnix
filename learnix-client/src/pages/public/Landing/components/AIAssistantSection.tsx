import { useTranslation } from 'react-i18next';

export function AIAssistantSection() {
    const { t } = useTranslation('landing');
    const features = t('aiAssistant.features', { returnObjects: true }) as string[];

    return (
        <section id="features" className="bg-foreground py-20 text-background">
            <div className="mx-auto grid max-w-7xl items-center gap-12 px-6 md:grid-cols-2">
                <div>
                    <span className="inline-flex items-center gap-2 rounded-full bg-accent/20 px-3 py-1.5 text-xs font-semibold uppercase tracking-wider text-accent">
                        {t('aiAssistant.badge')}
                    </span>
                    <h2 className="mt-5 font-heading text-4xl font-bold leading-tight md:text-5xl">
                        {t('aiAssistant.heading.line1')}
                        <br />
                        {t('aiAssistant.heading.line2')}
                        <br />
                        <span className="text-primary">{t('aiAssistant.heading.highlight')}</span>
                    </h2>
                    <p className="mt-6 max-w-lg text-lg leading-relaxed text-background/70">
                        {t('aiAssistant.subtitle')}
                    </p>
                    <ul className="mt-8 space-y-4 text-background/80">
                        {features.map((f) => (
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
                        <div className="grid h-8 w-8 place-items-center rounded-full bg-accent/20 text-sm text-accent">
                            ✨
                        </div>
                        <div>
                            <p className="font-heading text-sm font-semibold leading-none text-foreground">
                                {t('aiAssistant.chat.title')}
                            </p>
                            <p className="mt-1 flex items-center gap-1.5 text-[10px] text-muted-foreground">
                                <span className="h-1.5 w-1.5 rounded-full bg-success shadow-[0_0_5px_rgba(var(--success),0.8)]" />
                                {t('aiAssistant.chat.status')}
                            </p>
                        </div>
                    </div>

                    <div className="max-h-[420px] min-h-[340px] space-y-5 overflow-y-auto p-5">
                        <div className="flex justify-end">
                            <div className="max-w-[85%] rounded-2xl rounded-tr-sm bg-primary px-3.5 py-2.5 text-sm text-primary-foreground shadow-sm">
                                {t('aiAssistant.chat.messages.q1')}{' '}
                                <code className="rounded bg-primary-foreground/20 px-1.5 py-0.5 font-mono text-xs">
                                    {t('aiAssistant.chat.messages.q1Code')}
                                </code>{' '}
                                {t('aiAssistant.chat.messages.q1End')}
                            </div>
                        </div>
                        <div className="flex justify-start">
                            <div className="max-w-[85%] rounded-2xl rounded-tl-sm border border-border/50 bg-muted px-3.5 py-2.5 text-sm text-foreground shadow-sm">
                                {t('aiAssistant.chat.messages.a1')}{' '}
                                <strong>{t('aiAssistant.chat.messages.a1Bold')}</strong>{' '}
                                {t('aiAssistant.chat.messages.a1End')}
                                <div className="mt-2 text-xs text-muted-foreground">
                                    {t('aiAssistant.chat.messages.a1Note')}
                                </div>
                            </div>
                        </div>
                        <div className="flex justify-end">
                            <div className="max-w-[85%] rounded-2xl rounded-tr-sm bg-primary px-3.5 py-2.5 text-sm text-primary-foreground shadow-sm">
                                {t('aiAssistant.chat.messages.q2')}
                            </div>
                        </div>
                        <div className="flex justify-start">
                            <div className="max-w-[85%] rounded-2xl rounded-tl-sm border border-border/50 bg-muted px-3.5 py-2.5 text-sm text-foreground shadow-sm">
                                <span className="inline-flex gap-1 py-1">
                                    <span className="h-1.5 w-1.5 animate-pulse rounded-full bg-foreground/40" />
                                    <span
                                        className="h-1.5 w-1.5 animate-pulse rounded-full bg-foreground/40"
                                        style={{ animationDelay: '0.2s' }}
                                    />
                                    <span
                                        className="h-1.5 w-1.5 animate-pulse rounded-full bg-foreground/40"
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
                                placeholder={t('aiAssistant.chat.inputPlaceholder')}
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
