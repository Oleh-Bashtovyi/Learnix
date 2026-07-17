using Learnix.API.Constants;
using Learnix.Infrastructure.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Authorization.Policy;

namespace Learnix.API.Authorization;

/// <summary>
/// Gives a failed <c>[Authorize]</c> an RFC-7807 body carrying a machine-readable <c>code</c>.
/// </summary>
/// <remarks>
/// Authorization short-circuits in the middleware, before MVC — so <c>ResultExtensions</c>, which shapes
/// every handler-produced failure, never sees it, and the response would otherwise be a bare 403 with no
/// body at all. A bare 403 is unactionable: "wrong role" and "confirm your email" share the status but
/// need different UI, and the latter is what ADR-BACK-AUTH-014 promises a resend modal for.
///
/// The <c>code</c> is the contract, not <c>detail</c>: the client is localized (en/uk) and owns the
/// wording, so English prose written here could never be displayed.
///
/// Related ADRs:
/// - ADR-BACK-AUTH-018: the attribute owns the coarse role gate and must answer in ProblemDetails
/// - ADR-BACK-AUTH-014: the EmailConfirmed policy, whose failure this separates from a role failure
/// - ADR-BACK-AUTH-009: AuthenticationError (401) vs ForbiddenError (403)
/// </remarks>
public sealed class ProblemDetailsAuthorizationResultHandler : IAuthorizationMiddlewareResultHandler
{
    private const string ForbiddenTitle = "Forbidden";

    private readonly AuthorizationMiddlewareResultHandler _defaultHandler = new();

    public async Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        // A challenge (401) stays with the default handler, which defers to the JWT bearer scheme —
        // it owns the WWW-Authenticate header and its token-expiry description. A 401 also has only
        // one meaning here, and the client already drives its refresh flow off the status alone.
        if (!authorizeResult.Forbidden)
        {
            await _defaultHandler.HandleAsync(next, context, policy, authorizeResult);
            return;
        }

        await Results.Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: ForbiddenTitle,
                extensions: new Dictionary<string, object?>
                {
                    [AuthorizationFailureCodes.ExtensionKey] = ResolveCode(authorizeResult.AuthorizationFailure)
                })
            .ExecuteAsync(context);
    }

    private static string ResolveCode(AuthorizationFailure? failure)
    {
        // Forbid() can be called without a failure attached, e.g. from a custom policy handler.
        if (failure is null)
            return AuthorizationFailureCodes.Forbidden;

        var failedRequirements = failure.FailedRequirements.ToList();

        // Role is reported first when both fail: a missing role is not something the caller can act on,
        // so sending them to confirm their email would be a dead end.
        if (failedRequirements.OfType<RolesAuthorizationRequirement>().Any())
            return AuthorizationFailureCodes.InsufficientRole;

        if (failedRequirements
            .OfType<ClaimsAuthorizationRequirement>()
            .Any(requirement => requirement.ClaimType == ClaimNames.EmailVerified))
        {
            return AuthorizationFailureCodes.EmailNotConfirmed;
        }

        return AuthorizationFailureCodes.Forbidden;
    }
}
