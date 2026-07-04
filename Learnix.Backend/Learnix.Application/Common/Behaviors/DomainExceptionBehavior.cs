using FluentResults;
using Learnix.Application.Common.Errors;
using Learnix.Domain.Common.Exceptions;
using MediatR;

namespace Learnix.Application.Common.Behaviors;

public class DomainExceptionBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : IResultBase, new()
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        try
        {
            return await next();
        }
        catch (DomainException ex)
        {
            var response = new TResponse();

            response.Reasons.Add(new ConflictError(ex.Message));

            return response;
        }
    }
}
