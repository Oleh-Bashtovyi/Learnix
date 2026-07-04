using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Learnix.Application.Common.Behaviors;

public class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull, IRequest<TResponse>
    where TResponse : notnull
{
    private const int SlowRequestThresholdSeconds = 3;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        logger.LogInformation("[START] {Request}", requestName);

        var timer = Stopwatch.StartNew();
        var response = await next();
        timer.Stop();

        if (timer.Elapsed.TotalSeconds > SlowRequestThresholdSeconds)
        {
            logger.LogWarning(
                "[PERFORMANCE] {Request} took {ElapsedSeconds:F2}s",
                requestName, timer.Elapsed.TotalSeconds);
        }

        logger.LogInformation(
            "[END] {Request} completed in {ElapsedMs}ms",
            requestName, timer.ElapsedMilliseconds);

        return response;
    }
}
