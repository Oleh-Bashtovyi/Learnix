namespace Learnix.API.Constants;

/// <summary>
/// Machine-readable reasons for a 403, carried in the <c>code</c> extension of the
/// <c>ProblemDetails</c> body written by
/// <see cref="Authorization.ProblemDetailsAuthorizationResultHandler"/>.
/// </summary>
/// <remarks>
/// The client branches on these and supplies its own localized wording, so a value here is a contract
/// with the client: renaming one is a breaking API change, not a refactor.
///
/// Related ADRs:
/// - ADR-BACK-AUTH-018: the attribute owns the coarse role gate and answers in ProblemDetails
/// </remarks>
public static class AuthorizationFailureCodes
{
    /// <summary>Name of the <c>ProblemDetails</c> extension holding the code.</summary>
    public const string ExtensionKey = "code";

    /// <summary>The caller lacks a required role. Not resolvable by the caller.</summary>
    public const string InsufficientRole = "insufficient_role";

    /// <summary>The caller's email is unconfirmed. Resolvable — the client offers a resend.</summary>
    public const string EmailNotConfirmed = "email_not_confirmed";

    /// <summary>Authorization failed for a reason with no dedicated code.</summary>
    public const string Forbidden = "forbidden";
}
