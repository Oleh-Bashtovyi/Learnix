namespace Learnix.Application.Common.Abstractions.Identity;

/// <summary>
/// Provides info about the current authenticated user from the request context.
/// Reads JWT claims (see ADR-034). Returns null/empty values for unauthenticated requests.
/// Registered as scoped because HttpContext is request-scoped.
/// </summary>
public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? Email { get; }
    bool IsAuthenticated { get; }
    bool IsEmailConfirmed { get; }
    IReadOnlyList<string> GetRoles();
    bool IsInRole(string role);
}
