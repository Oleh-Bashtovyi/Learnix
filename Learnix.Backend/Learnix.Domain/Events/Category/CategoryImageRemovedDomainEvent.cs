using Learnix.Domain.Common;

namespace Learnix.Domain.Events.Category;

public sealed record CategoryImageRemovedDomainEvent(
    Guid CategoryId,
    string ImageBlobPath) : DomainEvent;
