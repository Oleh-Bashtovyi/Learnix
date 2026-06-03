namespace Learnix.Application.Sections.Abstractions;

public interface ISectionRepository
{
    Task BulkSetDisplayOrderAsync(IReadOnlyList<(Guid Id, int Order)> pairs, CancellationToken ct);
}
