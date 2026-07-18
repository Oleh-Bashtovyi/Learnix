import type { FieldError, FieldValues, Path, UseFormSetError } from 'react-hook-form';
import { AxiosError } from 'axios';
import i18n from '@/i18n/config';
import {
    AUTHORIZATION_FAILURE_CODES,
    type AuthorizationFailureCode,
    type ProblemDetails,
} from '@/types/api.types';

export function isValidationError(error: unknown): error is AxiosError<ProblemDetails> {
    return (
        error instanceof AxiosError &&
        error.response?.status === 400 &&
        !!error.response.data?.errors
    );
}

/**
 * A 404 means the resource genuinely does not exist — tell the user that, rather than
 * offering a retry that can never succeed.
 */
export function isNotFoundError(error: unknown): error is AxiosError<ProblemDetails> {
    return error instanceof AxiosError && error.response?.status === 404;
}

/**
 * i18n key (under the `common:authorization` namespace) for each 403 `code`. The codes carry
 * underscores; the keys are camelCase to match the JSON convention and avoid colliding with
 * i18next's `_`-suffixed plural forms.
 */
const AUTHORIZATION_MESSAGE_KEYS: Record<AuthorizationFailureCode, string> = {
    insufficient_role: 'insufficientRole',
    email_not_confirmed: 'emailNotConfirmed',
    forbidden: 'forbidden',
};

/**
 * The machine-readable `code` on a 403, when the backend sent one (ADR-BACK-AUTH-018). The client
 * branches on this — never on the response's `detail`/`title`, which are hardcoded English.
 */
export function getAuthorizationFailureCode(error: unknown): AuthorizationFailureCode | undefined {
    if (!(error instanceof AxiosError) || error.response?.status !== 403) return undefined;

    const code = (error.response.data as ProblemDetails | undefined)?.code;
    return AUTHORIZATION_FAILURE_CODES.includes(code as AuthorizationFailureCode)
        ? (code as AuthorizationFailureCode)
        : undefined;
}

export function getErrorMessage(error: unknown, fallback = 'Something went wrong'): string {
    // A 403 carries a code, not displayable prose — its wording is ours to own and localize.
    const authCode = getAuthorizationFailureCode(error);
    if (authCode) {
        return i18n.t(`authorization.${AUTHORIZATION_MESSAGE_KEYS[authCode]}`, { ns: 'common' });
    }

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
