namespace Learnix.Application.Common.Models;

/// <summary>
/// Single item in a bulk reorder payload. Used by ReorderSectionsCommand and ReorderLessonsCommand.
/// </summary>
public sealed record ReorderItem(Guid Id, int Order);
