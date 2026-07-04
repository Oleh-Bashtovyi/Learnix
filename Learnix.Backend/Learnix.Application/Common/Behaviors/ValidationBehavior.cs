using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using FluentResults;
using FluentValidation;
using FluentValidation.Results;
using Learnix.Application.Common.Errors;
using MediatR;

namespace Learnix.Application.Common.Behaviors;

/// <summary>
/// Caches a compiled <see cref="Func{ValidationError,TResponse}"/> per <em>response type</em>.
/// Reflection runs at most once per unique TResponse, regardless of how many TRequest types share it.
/// The method reference is obtained via an expression tree — no hardcoded strings, compile-time safe.
/// </summary>
internal static class ResultFailFactory
{
    private static readonly ConcurrentDictionary<Type, Delegate> Cache = new();

    // Capture Result.Fail<object>(IError) at compile time — no "Fail" string literal.
    // GetGenericMethodDefinition() gives us the open Result.Fail<T>(IError) overload.
    private static readonly MethodInfo OpenFailMethod =
        ((MethodCallExpression)((Expression<Func<IError, Result<object>>>)(e => Result.Fail<object>(e))).Body)
        .Method
        .GetGenericMethodDefinition();

    internal static Func<ValidationError, TResponse> Get<TResponse>()
        where TResponse : IResultBase
        => (Func<ValidationError, TResponse>)Cache.GetOrAdd(typeof(TResponse), _ => Build<TResponse>());

    private static Delegate Build<TResponse>() where TResponse : IResultBase
    {
        var type = typeof(TResponse);

        // Non-generic Result — no reflection needed
        if (type == typeof(Result))
            return (Func<ValidationError, TResponse>)(error => (TResponse)(object)Result.Fail(error));

        // Result<T> — bind the open method to T, then compile to IL (no Invoke overhead)
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Result<>))
        {
            var typeArg = type.GetGenericArguments()[0];
            var bound = OpenFailMethod.MakeGenericMethod(typeArg);

            var param = Expression.Parameter(typeof(ValidationError), "error");
            var call = Expression.Call(bound, Expression.Convert(param, typeof(IError)));
            var funcType = typeof(Func<,>).MakeGenericType(typeof(ValidationError), type);
            return Expression.Lambda(funcType, call, param).Compile();
        }

        throw new InvalidOperationException(
            $"ResultFailFactory does not support '{type.Name}'. Use Result or Result<T>.");
    }
}

/// <remarks>
/// Related ADRs:
/// - ADR-BACK-ARCH-003: FluentValidation + FluentResults in pipeline (no exceptions)
/// </remarks>
public class ValidationBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : IResultBase
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
        => _validators = validators;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);
        var results = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = results
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count == 0)
            return await next();

        return ResultFailFactory.Get<TResponse>()(
            new ValidationError(new ValidationResult(failures)));
    }
}
