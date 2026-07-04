using Learnix.Domain.Common;
using Learnix.Domain.Events.Category;

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
    /// System categories are seeded on startup and cannot be deleted.
    /// </summary>
    public bool IsSystem { get; private set; }

    public string? ImageBlobPath { get; private set; }

    public int CoursesCount { get; private set; }

    public static Category Create(string name, string slug)
        => new(name, slug, isSystem: false);

    public static Category CreateSystem(string name, string slug)
        => new(name, slug, isSystem: true);

    public void Rename(string name, string slug)
    {
        Name = name;
        Slug = slug;
    }

    public void SetImage(string blobPath)
    {
        if (ImageBlobPath == blobPath)
            return;

        if (ImageBlobPath is not null)
            RaiseDomainEvent(new CategoryImageRemovedDomainEvent(Id, ImageBlobPath));

        ImageBlobPath = blobPath;
        RaiseDomainEvent(new CategoryImageSetDomainEvent(Id, ImageBlobPath));
    }

    public void RemoveImage()
    {
        if (ImageBlobPath is not null)
        {
            RaiseDomainEvent(new CategoryImageRemovedDomainEvent(Id, ImageBlobPath));
            ImageBlobPath = null;
        }
    }

    public override void PrepareForDeletion()
    {
        base.PrepareForDeletion();
        RemoveImage();
    }

    public void IncrementCoursesCount() => CoursesCount++;
    public void DecrementCoursesCount() => CoursesCount = Math.Max(0, CoursesCount - 1);
}
