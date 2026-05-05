import { AxiosError } from 'axios';
import type { UseFormSetError, FieldValues, Path } from 'react-hook-form';
import type { ProblemDetails } from '@/types/api.types';

export function isValidationError(error: unknown): error is AxiosError<ProblemDetails> {
    return (
        error instanceof AxiosError &&
        error.response?.status === 400 &&
        !!error.response.data?.errors
    );
}

export function getErrorMessage(error: unknown, fallback = 'Something went wrong'): string {
    if (error instanceof AxiosError) {
        const problem = error.response?.data as ProblemDetails | undefined;
        return problem?.detail ?? problem?.title ?? error.message ?? fallback;
    }
    return fallback;
}

/**
 * Maps ProblemDetails field errors (PascalCase keys) onto RHF form fields (camelCase keys).
 * Unmatched fields are surfaced as a root error.
 */
export function setApiFieldErrors<T extends FieldValues>(
    error: unknown,
    setError: UseFormSetError<T>,
    fieldMap: Partial<Record<string, Path<T>>>,
): void {
    if (!isValidationError(error)) return;

    const apiErrors = error.response!.data.errors ?? {};
    let hasUnmapped = false;

    for (const [apiKey, messages] of Object.entries(apiErrors)) {
        const formField = fieldMap[apiKey] ?? fieldMap[apiKey.toLowerCase()];
        if (formField) {
            setError(formField, { type: 'server', message: messages[0] });
        } else {
            hasUnmapped = true;
        }
    }

    if (hasUnmapped) {
        const firstMessages = Object.values(apiErrors).flat();
        setError('root' as Path<T>, { type: 'server', message: firstMessages[0] });
    }
}
