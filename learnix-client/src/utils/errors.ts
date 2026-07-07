import type { FieldError, FieldValues, Path, UseFormSetError } from 'react-hook-form';
import { AxiosError } from 'axios';
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

export type ApiFieldMap<TApiDto, TForm extends FieldValues> = Partial<
    Record<keyof TApiDto, Path<TForm>>
>;

export function getFieldErrors(error: FieldError | string | undefined): string[] {
    if (!error) return [];
    if (typeof error === 'string') return [error];
    if (error.types) return Object.values(error.types).flat().map(String);
    if (error.message) return [error.message];
    return [];
}

/**
 * Maps ProblemDetails field errors onto RHF form fields safely.
 * Allows strongly-typed mapping while supporting case-insensitive API keys.
 * Unmatched fields are surfaced as a root error.
 *
 * Related ADRs:
 * - ADR-FRONT-FORMS-003: Server-to-Client Validation Mapping
 */
export function setApiFieldErrors<TApiDto, TForm extends FieldValues>(
    error: unknown,
    setError: UseFormSetError<TForm>,
    fieldMap: ApiFieldMap<TApiDto, TForm>,
): void {
    if (!isValidationError(error)) return;

    const apiErrors = error.response!.data.errors ?? {};
    let hasUnmapped = false;

    // Create a lowercased map for runtime lookup to handle API casing inconsistencies
    const normalizedMap = new Map<string, Path<TForm>>();

    for (const [key, val] of Object.entries(fieldMap)) {
        if (val) normalizedMap.set(key.toLowerCase(), val as Path<TForm>);
    }

    for (const [apiKey, messages] of Object.entries(apiErrors)) {
        const formField = normalizedMap.get(apiKey.toLowerCase());

        if (formField) {
            const types = messages.reduce(
                (acc, msg, idx) => {
                    acc[`server_${idx}`] = msg;
                    return acc;
                },
                {} as Record<string, string>,
            );

            setError(formField, { type: 'server', message: messages[0], types });
        } else {
            hasUnmapped = true;
        }
    }

    if (hasUnmapped) {
        const firstMessages = Object.values(apiErrors).flat();
        setError('root' as Path<TForm>, { type: 'server', message: firstMessages[0] });
    }
}

/**
 * Standardized form error handling for try-catch blocks.
 * Maps API errors if possible, otherwise sets a generic root error.
 */
export function handleFormError<TApiDto, TForm extends FieldValues>(
    error: unknown,
    setError: UseFormSetError<TForm>,
    fallbackMessage: string,
    fieldMap?: ApiFieldMap<TApiDto, TForm>,
): void {
    if (isValidationError(error) && fieldMap) {
        setApiFieldErrors(error, setError, fieldMap);
    } else {
        setError('root' as Path<TForm>, {
            type: 'server',
            message: getErrorMessage(error, fallbackMessage),
        });
    }
}
