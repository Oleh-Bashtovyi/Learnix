using FluentResults;

namespace Learnix.Application.Common.Errors;

public class NotFoundError : Error
{
    public NotFoundError(string message) : base(message) { }
}
