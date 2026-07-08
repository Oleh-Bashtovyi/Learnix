interface AuthDividerProps {
    text: string;
}

export function AuthDivider({ text }: AuthDividerProps) {
    return (
        <div className="my-4 flex items-center gap-3">
            <div className="h-px flex-1 bg-border" />
            <span className="text-xs text-muted-foreground">{text}</span>
            <div className="h-px flex-1 bg-border" />
        </div>
    );
}
