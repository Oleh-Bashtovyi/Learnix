namespace Learnix.Domain.Common;

/// <summary>
/// Shared invariants for bulk reorder operations. Used by Course.ReorderSections
/// and Section.ReorderLessons to avoid duplicating set-equality / uniqueness logic.
/// </summary>
internal static class ReorderValidation
{
    public static void EnsureValid(
        IReadOnlyList<(Guid Id, int Order)> pairs,
        IEnumerable<Guid> existingIds,
        string entityName)
    {
        if (pairs.Count == 0)
            throw new InvalidOperationException($"Reorder payload cannot be empty.");

        var payloadIds = pairs.Select(p => p.Id).ToHashSet();
        if (payloadIds.Count != pairs.Count)
            throw new InvalidOperationException($"Duplicate {entityName} IDs in reorder payload.");

        var payloadOrders = pairs.Select(p => p.Order).ToHashSet();
        if (payloadOrders.Count != pairs.Count)
            throw new InvalidOperationException($"Duplicate order values in reorder payload.");

        if (pairs.Any(p => p.Order < 0))
            throw new InvalidOperationException("Order values must be non-negative.");

        var existingSet = existingIds.ToHashSet();
        if (!existingSet.SetEquals(payloadIds))
            throw new InvalidOperationException(
                $"Reorder payload must contain exactly all existing {entityName}s — no missing and no extra.");
    }
}
