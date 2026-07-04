using FluentResults;

namespace Learnix.Application.Common.Errors;

public class BlobValidationError : Error
{
    public BlobValidationError(string message) : base(message) { }
}
