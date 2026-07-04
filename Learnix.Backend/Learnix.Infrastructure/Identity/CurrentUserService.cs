using System.Security.Claims;
using Learnix.Application.Common.Abstractions.Identity;
using Microsoft.AspNetCore.Http;

namespace Learnix.Infrastructure.Identity;

/// <summary>
/// Reads current user from JWT claims (see ADR-034 for claim name mapping).
/// Registered as scoped because HttpContext is request-scoped.
/// </summary>
public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

    public Guid? UserId
    {
        get
        {
            var sub = User?.FindFirstValue("sub");
            return Guid.TryParse(sub, out var id) ? id : null;
        }
    }

    public string? Email => User?.FindFirstValue("email");

    public bool IsEmailConfirmed =>
        User?.FindFirstValue("email_verified") == "true";

    public IReadOnlyList<string> GetRoles() =>
        User?.FindAll("role").Select(c => c.Value).ToList() ?? [];

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated == true;

    public bool IsInRole(string role) => User?.IsInRole(role) == true;
}
