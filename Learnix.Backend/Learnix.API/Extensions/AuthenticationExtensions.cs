using System.Text;
using Learnix.API.Authorization;
using Learnix.API.Constants;
using Learnix.Application.Common.Options;
using Learnix.Infrastructure.Constants;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;

namespace Learnix.API.Extensions;

/// <remarks>
/// Related ADRs:
/// - ADR-BACK-AUTH-001: JWT (short-lived) + Refresh Token
/// - ADR-BACK-AUTH-008: JWT claims — standard OIDC + custom for roles
/// - ADR-BACK-AUTH-014: Email confirmation soft restriction (email_verified claim)
/// </remarks>
public static class AuthenticationExtensions
{
    /// <summary>
    /// Wires the HTTP authentication/authorization pipeline: JWT bearer validation and
    /// the authorization policies consumed by <c>[Authorize]</c> attributes. Token issuing
    /// itself lives in Infrastructure (<c>ITokenService</c>).
    /// </summary>
    public static IServiceCollection AddLearnixAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtOptions = configuration.GetSection(ConfigurationSectionNameConstants.Jwt).Get<JwtOptions>()
            ?? throw new InvalidOperationException(
                $"Configuration section '{ConfigurationSectionNameConstants.Jwt}' is missing.");

        if (string.IsNullOrWhiteSpace(jwtOptions.Secret))
            throw new InvalidOperationException("JWT secret is not configured.");

        services
            .AddAuthentication(options =>
            {
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidAudience = jwtOptions.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Secret)),
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = ClaimNames.Name,
                    RoleClaimType = ClaimNames.Role
                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        // A WebSocket handshake cannot carry an Authorization header,
                        // so SignalR clients pass the token in the query string instead.
                        var token = context.Request.Query["access_token"].ToString();
                        if (!string.IsNullOrEmpty(token) &&
                            context.HttpContext.Request.Path.StartsWithSegments(HubRoutes.Prefix))
                        {
                            context.Token = token;
                        }
                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorizationBuilder()
            .AddPolicy(AuthPolicies.EmailConfirmed, policy =>
                policy.RequireClaim(ClaimNames.EmailVerified, ClaimNames.TrueValue));

        // Without this, a failed [Authorize] short-circuits before MVC and answers with a bodyless 403,
        // leaving the client unable to tell a role failure from an unconfirmed email (ADR-BACK-AUTH-018).
        services.AddSingleton<IAuthorizationMiddlewareResultHandler, ProblemDetailsAuthorizationResultHandler>();

        return services;
    }
}
