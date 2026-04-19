using FluentResults;
using Learnix.Application.Common.Errors;
using Microsoft.AspNetCore.Mvc;

namespace Learnix.API.Extensions;

public static class ResultExtensions
{
    public static IActionResult ToActionResult(this Result result)
        => result.IsSuccess
            ? new NoContentResult()
            : MapFailure(result.Errors);

    public static IActionResult ToActionResult<T>(
        this Result<T> result,
        Func<T, IActionResult>? onSuccess = null)
    {
        if (result.IsSuccess)
        {
            return onSuccess is not null
                ? onSuccess(result.Value)
                : new OkObjectResult(result.Value);
        }

        return MapFailure(result.Errors);
    }

    private static IActionResult MapFailure(IReadOnlyCollection<IError> errors)
    {
        // ValidationError → 400 with ValidationProblemDetails
        var validationError = errors.OfType<ValidationError>().FirstOrDefault();
        if (validationError is not null)
        {
            return new BadRequestObjectResult(
                new ValidationProblemDetails(validationError.ToDictionary()));
        }

        if (errors.Any(e => e is NotFoundError))
            return Problem(StatusCodes.Status404NotFound, "Resource not found", errors);

        if (errors.Any(e => e is ConflictError))
            return Problem(StatusCodes.Status409Conflict, "Conflict", errors);

        if (errors.Any(e => e is AuthenticationError))
            return Problem(StatusCodes.Status401Unauthorized, "Authentication failed", errors);

        if (errors.Any(e => e is ForbiddenError))
            return Problem(StatusCodes.Status403Forbidden, "Forbidden", errors);

        return Problem(StatusCodes.Status400BadRequest, "Bad request", errors);
    }

    private static ObjectResult Problem(int status, string title, IEnumerable<IError> errors)
    {
        var problemDetails = new ProblemDetails
        {
            Title = title,
            Status = status,
            Detail = string.Join("; ", errors.Select(e => e.Message))
        };

        return new ObjectResult(problemDetails) { StatusCode = status };
    }
}