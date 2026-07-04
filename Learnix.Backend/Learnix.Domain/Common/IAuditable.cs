namespace Learnix.Domain.Common;

public interface IAuditable
{
    DateTime CreatedAt { get; }
    DateTime UpdatedAt { get; }
}
