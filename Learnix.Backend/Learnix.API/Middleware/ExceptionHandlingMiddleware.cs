using Microsoft.AspNetCore.Mvc;

namespace Learnix.API.Middleware;

public sealed class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger,
    IHostEnvironment environment)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (OperationCanceledException ex) when (context.RequestAborted.IsCancellationRequested)
        {
            // The client went away (a closed tab on the AI chat SSE stream is the common case) — not a
            // bug, and there is nobody left to write a response to.
            logger.LogDebug(
                ex,
                "Request aborted by the client for {Method} {Path}",
                context.Request.Method, context.Request.Path);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Unhandled exception for {Method} {Path}",
                context.Request.Method, context.Request.Path);

            // A response already in flight (SSE headers/chunks already written, e.g. AiChatController's
            // stream) cannot have its status code or body rewritten — attempting to would throw a second,
            // masking exception instead of the one just logged.
            if (context.Response.HasStarted)
            {
                logger.LogWarning(
                    "Response already started for {Method} {Path}; cannot write the error body.",
                    context.Request.Method, context.Request.Path);
                return;
            }

            await WriteProblemDetailsAsync(context, ex);
        }
    }

    private async Task WriteProblemDetailsAsync(HttpContext context, Exception ex)
    {
        var problem = new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc9110#section-15.6.1",
            Title = "An unexpected error occurred.",
            Status = StatusCodes.Status500InternalServerError,
            Instance = context.Request.Path
        };

        if (environment.IsDevelopment())
        {
            problem.Extensions["stackTrace"] = ex.ToString();
        }

        problem.Extensions["traceId"] = context.TraceIdentifier;

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/problem+json";

        await context.Response.WriteAsJsonAsync(problem, context.RequestAborted);
    }
}
