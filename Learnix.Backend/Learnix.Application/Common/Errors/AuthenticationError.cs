using FluentResults;

namespace Learnix.Application.Common.Errors;

public sealed class AuthenticationError : Error
{
    public AuthenticationError(string message) : base(message) { }
}
