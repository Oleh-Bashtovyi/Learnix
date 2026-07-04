namespace Learnix.API.Middleware;

public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;

        // Prevents browsers from guessing the MIME type, forcing them to strictly trust the provided Content-Type
        headers.Append("X-Content-Type-Options", "nosniff");

        // Blocks rendering of the response in a <frame>, <iframe>, or <object> to prevent clickjacking attacks
        headers.Append("X-Frame-Options", "DENY");

        // Sends full referrer URL for same-origin requests, but only the domain for secure cross-origin requests
        headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");

        // Restricts legacy clients like Adobe Flash and PDF documents from making cross-domain requests
        headers.Append("X-Permitted-Cross-Domain-Policies", "none");

        // CSP for API: blocks loading of any external resources (default-src 'none') and prevents embedding (frame-ancestors 'none')
        headers.Append("Content-Security-Policy", "default-src 'none'; frame-ancestors 'none'");

        // Passes the HTTP context to the next middleware in the application pipeline
        await next(context);
    }
}
