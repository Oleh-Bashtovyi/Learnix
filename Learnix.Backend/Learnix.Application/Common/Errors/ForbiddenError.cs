using FluentResults;

namespace Learnix.Application.Common.Errors;

public class ForbiddenError : Error
{
    public ForbiddenError(string message) : base(message) { }
}
