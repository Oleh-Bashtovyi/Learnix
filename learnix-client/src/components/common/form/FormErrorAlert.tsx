interface FormErrorAlertProps {
    message?: string;
}

export function FormErrorAlert({ message }: FormErrorAlertProps) {
    if (!message) return null;

    return (
        <div className="rounded-lg border border-destructive/20 bg-destructive/5 px-4 py-3 text-sm text-destructive">
            {message}
        </div>
    );
}
