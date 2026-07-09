namespace Learnix.Application.Courses.Constants;

public static class CourseValidationConstants
{
    /// <summary>
    /// Max length of the public catalog search term.
    ///
    /// This bound is a cache concern as much as a validation one: the term is embedded in the
    /// distributed-cache key for <c>GetPublicCoursesQuery</c>, and the catalog endpoint is
    /// anonymous. Without a ceiling, an unauthenticated caller can mint unbounded Redis entries.
    /// </summary>
    public const int SearchMaxLength = 100;
}
