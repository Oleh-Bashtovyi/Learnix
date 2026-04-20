using Learnix.Domain.Common;
using Learnix.Domain.Common.Exceptions;

namespace Learnix.Domain.Entities;

public class Category : BaseEntity
{
    private Category() { }

    private Category(string name, string slug, bool isSystem)
    {
        Name = name;
        Slug = slug;
        IsSystem = isSystem;
    }

    public string Name { get; private set; } = null!;
    public string Slug { get; private set; } = null!;

    /// <summary>
    /// System categories are seeded on startup and cannot be renamed or deleted.
    /// </summary>
    public bool IsSystem { get; private set; }

    public static Category Create(string name, string slug)
        => new(name, slug, isSystem: false);

    public static Category CreateSystem(string name, string slug)
        => new(name, slug, isSystem: true);

    public void Rename(string name, string slug)
    {
        if (IsSystem)
            throw new DomainException(
                $"Cannot rename system category '{Name}'.");

        Name = name;
        Slug = slug;
    }
}
