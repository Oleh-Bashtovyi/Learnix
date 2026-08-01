namespace Learnix.Application.Courses.Constants;

public static class CourseTagConstants
{
    /// <summary>How many suggestions the course editor is offered at most.</summary>
    public const int PopularTagsLimit = 20;

    /// <summary>
    /// How many published courses must already carry a tag before it counts as popular. A tag used
    /// once is one author's private vocabulary, not a convention worth spreading to other courses.
    /// </summary>
    public const int PopularTagsMinCourses = 2;
}
