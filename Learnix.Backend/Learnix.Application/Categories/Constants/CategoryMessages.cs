namespace Learnix.Application.Categories.Constants;

public static class CategoryMessages
{
    public static string CategorySlugInUse(string slug) => $"Category slug '{slug}' is already in use.";
    public static string CannotDeleteWithCourses => "Cannot delete category because it contains courses.";
    public static string SystemCategoriesCannotBeDeleted => "System categories cannot be deleted.";
    public static string CategoryUsedByCourses => "Category is used by existing courses and cannot be deleted.";
    public static string CategoryHasNoImage => "Category has no image.";
}
