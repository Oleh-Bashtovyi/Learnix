using Learnix.Domain.Common;

namespace Learnix.Domain.Events.Category;

public sealed record CategoryImageSetDomainEvent(
    Guid CategoryId,
    string ImageBlobPath) : DomainEvent;
