/**
 * RFC 7807 ProblemDetails — backend error format.
 */
export interface ProblemDetails {
    type?: string;
    title?: string;
    status?: number;
    detail?: string;
    instance?: string;
    errors?: Record<string, string[]>;
    /**
     * Machine-readable reason carried on a 403, written by the backend's
     * `ProblemDetailsAuthorizationResultHandler` (ADR-BACK-AUTH-018). The client branches on this
     * and supplies its own localized wording — never on `detail`/`title`, which are hardcoded English.
     */
    code?: string;
    [key: string]: unknown;
}

/**
 * The set of `code` values a 403 can carry. Mirrors the backend's `AuthorizationFailureCodes`
 * (ADR-BACK-AUTH-018) — a value here is a contract with the server.
 */
export const AUTHORIZATION_FAILURE_CODES = [
    'insufficient_role',
    'email_not_confirmed',
    'forbidden',
] as const;

export type AuthorizationFailureCode = (typeof AUTHORIZATION_FAILURE_CODES)[number];

/**
 * Related ADRs:
 * - ADR-FRONT-API-005: Type Definition Strategy (Manual vs Codegen)
 *
 * Generic paginated wrapper from backend.
 */
export interface PaginatedResult<T> {
    items: T[];
    totalCount: number;
    page: number;
    pageSize: number;
    totalPages: number;
    hasNextPage: boolean;
    hasPreviousPage: boolean;
}

export interface PaginationRequest {
    page: number;
    pageSize: number;
}
