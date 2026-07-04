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
    [key: string]: unknown;
}

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
